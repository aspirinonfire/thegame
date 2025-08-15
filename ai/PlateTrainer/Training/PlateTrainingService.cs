using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using System.Text.Json;

namespace PlateTrainer.Training;

public sealed class PlateTrainingService(MLContext mlContext, int numOfIterations = 200, float l2Reg = 0.001f)
{
  private const string _rawTokenColumn = "RawTokens";
  private const string _cleanTokenColumn = "CleanTokens";
  private const string _cleanTokenKeyColumn = "TokenKeys";
  private const string _featuresColumn = "Features";
  private const string _labelKeyColumn = "LabelKey";

  /// <summary>
  /// See <see href="https://learn.microsoft.com/en-us/azure/machine-learning/algorithm-cheat-sheet?view=azureml-api-1"/>
  /// </summary>
  /// <returns></returns>
  public EstimatorPipelineParts CreateEstimatorPipeline()
  {
    var featurizer = mlContext.Transforms.Text.TokenizeIntoWords(
        inputColumnName: nameof(PlateTrainingRow.Text),
        outputColumnName: _rawTokenColumn,
        separators: [',', ' '])
      .Append(mlContext.Transforms.Text.RemoveDefaultStopWords(
        inputColumnName: _rawTokenColumn,
        outputColumnName: _cleanTokenColumn,
        language: StopWordsRemovingEstimator.Language.English))
      // text transforms (producengram) works with numeric Id not strings, so we need to convert clean tokens to Ids.
      .Append(mlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: _cleanTokenColumn,
        outputColumnName: _cleanTokenKeyColumn))
      .Append(mlContext.Transforms.Text.ProduceNgrams(
        inputColumnName: _cleanTokenKeyColumn,
        outputColumnName: _featuresColumn,
        ngramLength: 4,
        useAllLengths: true,
        weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf))
      // trainers work with numeric label Ids not strings.
      // To avoid potential ONNX column name collisions - map to a new column
      .Append(mlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: nameof(PlateTrainingRow.Label),
        outputColumnName: _labelKeyColumn));

    // trainers produce predicted label as Id not string, convert it to human readable.
    var predictedLabelKey = mlContext.Transforms.Conversion.MapKeyToValue(
      inputColumnName: nameof(PlatePrediction.PredictedLabel),
      outputColumnName: nameof(PlatePrediction.PredictedLabel));

    var trainerOpts = new SdcaMaximumEntropyMulticlassTrainer.Options()
    {
      LabelColumnName = _labelKeyColumn,
      FeatureColumnName = _featuresColumn,
      ExampleWeightColumnName = nameof(PlateTrainingRow.Weight),
      MaximumNumberOfIterations = numOfIterations,
      L2Regularization = l2Reg,
      Shuffle = true,
      NumberOfThreads = Environment.ProcessorCount
    };

    var trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(trainerOpts);

    return new EstimatorPipelineParts(featurizer, trainer, trainer, predictedLabelKey);
  }

  public TrainedModel Train(IDataView dataView)
  {
    Console.WriteLine("----- Training...");

    var parts = CreateEstimatorPipeline();

    // Fit featurizer + labelKey
    var featurizerModel = parts.Featurizer.Fit(dataView);
    var featurizedDataView = featurizerModel.Transform(dataView);

    var labelKeyModel = parts.LabelKey.Fit(featurizedDataView);
    var preppedDataView = labelKeyModel.Transform(featurizedDataView);

    // Train the head (this is the only real "training")
    var head = parts.Trainer.Fit(preppedDataView);

    // IMPORTANT: Fit MapKeyToValue on the *scored* view so PredictedLabel (key) exists
    var scored = head.Transform(preppedDataView);
    var predictedLabelMapModel = parts.PredictedLabelMap.Fit(scored);

    // Compose final transformer chain
    var trainedModel = featurizerModel
      .Append(labelKeyModel)
      .Append(head)
      .Append(predictedLabelMapModel);

    Console.WriteLine("----- Training completed successfully.");

    // map key indices -> label strings taken from the *Label* column
    var outputSchema = trainedModel.GetOutputSchema(dataView.Schema);
    var labelCol = outputSchema[_labelKeyColumn];
    var keyBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    labelCol.GetKeyValues(ref keyBuffer);
    var labels = keyBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();


    return new(trainedModel,
      labels,
      featurizerModel,
      head.Model,
      _featuresColumn);
  }

  public void ExportToOnnx(TrainedModel trainedModel,
    string onnxPath,
    string labelsJsonPath)
  {
    // Export ONNX with only the "Score" output to simplify client post-processing.

    var schemaView = mlContext.Data.LoadFromEnumerable(
      [
        new PlateTrainingRow("onnx", "export", 0)
      ]);

    using (var stream = File.Create(onnxPath))
    {
      mlContext.Model.ConvertToOnnx(trainedModel.FullTrainedModel, schemaView, stream, "Score");
    }

    File.WriteAllText(labelsJsonPath, JsonSerializer.Serialize(trainedModel.Labels));
  }

  public sealed record EstimatorPipelineParts(
    IEstimator<ITransformer> Featurizer,
    IEstimator<ITransformer> LabelKey,
    IEstimator<MulticlassPredictionTransformer<MaximumEntropyModelParameters>> Trainer,
    IEstimator<ITransformer> PredictedLabelMap);
}

public sealed record TrainedModel(ITransformer FullTrainedModel,
  string[] Labels,
  ITransformer Featurizer,
  MaximumEntropyModelParameters HeadModel,
  string FeatureColumnName);
