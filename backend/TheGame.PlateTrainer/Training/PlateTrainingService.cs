using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using System.Text.Json;

namespace TheGame.PlateTrainer.Training;

public sealed class PlateTrainingService(MLContext mlContext, int numOfIterations = 200, float l2Reg = 0.001f)
{
  private const string _rawTokenColumn = "RawTokens";
  private const string _cleanTokenColumn = "CleanTokens";
  private const string _cleanTokenKeyColumn = "TokenKeys";
  private const string _featuresColumn = "Features";

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
        ngramLength: 2,
        useAllLengths: true,
        weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf))
      // trainers work with numeric label Ids not strings.
      // To avoid potential ONNX column name collisions - map to a new column
      .Append(mlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: nameof(PlateTrainingRow.Label),
        outputColumnName: nameof(PlateTrainingRow.Label)));

    // trainers produce predicted label as Id not string, convert it to human readable.


    var trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options()
    {
      LabelColumnName = nameof(PlateTrainingRow.Label),
      FeatureColumnName = _featuresColumn,
      MaximumNumberOfIterations = numOfIterations,
      L2Regularization = l2Reg,
      Shuffle = true,
      NumberOfThreads = Environment.ProcessorCount
    });

    return new EstimatorPipelineParts(featurizer, trainer);
  }

  public TrainedModel Train(IDataView dataView)
  {
    Console.WriteLine("----- Training...");

    var parts = CreateEstimatorPipeline();

    var predictedLabelKey = mlContext.Transforms.Conversion.MapKeyToValue(
      inputColumnName: nameof(PlatePrediction.PredictedLabel),
      outputColumnName: nameof(PlatePrediction.PredictedLabel));

    var trainedModel = parts.Featurizer
      //.Append(parts.LabelKey)
      .Append(parts.Trainer)
      .Append(predictedLabelKey)
      .Fit(dataView);

    Console.WriteLine("----- Training completed successfully.");

    // map key indices -> label strings taken from the *Label* column
    var outputSchema = trainedModel.GetOutputSchema(dataView.Schema);
    var labelCol = outputSchema[nameof(PlateTrainingRow.Label)];
    var keyBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    labelCol.GetKeyValues(ref keyBuffer);
    var labels = keyBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();

    return new(trainedModel,
      labels,
      parts.Featurizer);
  }

  public sealed record EstimatorPipelineParts(IEstimator<ITransformer> Featurizer,
    IEstimator<MulticlassPredictionTransformer<MaximumEntropyModelParameters>> Trainer);
}

public sealed record TrainedModel(ITransformer Model,
  string[] Labels,
  IEstimator<ITransformer> Featurizer);
