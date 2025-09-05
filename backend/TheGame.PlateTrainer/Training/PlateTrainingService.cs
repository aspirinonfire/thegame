using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using System.Text.Json;
using TheGame.PlateTrainer.Prediction;
using TheGame.PlateTrainer.Validation;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        inputColumnName: nameof(PlateRow.Text),
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
        inputColumnName: nameof(PlateRow.Label),
        outputColumnName: nameof(PlateRow.Label)));

    // trainers produce predicted label as Id not string, convert it to human readable.


    var trainer = mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options()
    {
      LabelColumnName = nameof(PlateRow.Label),
      FeatureColumnName = _featuresColumn,
      MaximumNumberOfIterations = numOfIterations,
      L2Regularization = l2Reg,
      Shuffle = true,
      NumberOfThreads = Environment.ProcessorCount
    });

    return new EstimatorPipelineParts(featurizer, trainer);
  }

  public TrainedModel Train(IDataView trainDataView, int mlSeed)
  {

    var parts = CreateEstimatorPipeline();

    var predictedLabelKey = mlContext.Transforms.Conversion.MapKeyToValue(
      inputColumnName: nameof(PlatePrediction.PredictedLabel),
      outputColumnName: nameof(PlatePrediction.PredictedLabel));

    var estimator = parts.Featurizer
      //.Append(parts.LabelKey)
      .Append(parts.Trainer)
      .Append(predictedLabelKey);

    Console.WriteLine("----- Cross Validating...");
    
    var folds = mlContext.Data.CrossValidationSplit(trainDataView, numberOfFolds: 5, seed: mlSeed);

    var foldMetrics = folds
      .Select(fold =>
      {
        var model = estimator.Fit(fold.TrainSet);
        var scored = model.Transform(fold.TestSet);

        var rows = mlContext.Data
          .CreateEnumerable<CvFoldScores>(scored,
            reuseRowObject: false,
            ignoreMissingColumns: false)
          .ToArray();

        var evals = mlContext.MulticlassClassification.Evaluate(
          scored,
          labelColumnName: "Label",
          scoreColumnName: "Score",
          predictedLabelColumnName: "PredictedLabel",
          topKPredictionCount: 10
        );

        return new
        {
          evals.MicroAccuracy,
          evals.MacroAccuracy,
          evals.LogLoss,
          evals.TopKAccuracy,
          NDCG = TrainedModelValidationService.CalculateNdcg(rows, 10)
        };
      })
      .ToList();

    var cvMicro = foldMetrics.Average(f => f.MicroAccuracy);
    var cvMacro = foldMetrics.Average(f => f.MacroAccuracy);
    var topK = foldMetrics.Average(f => f.TopKAccuracy);
    var cvLogLoss = foldMetrics.Average(f => f.LogLoss);
    var ndcg = foldMetrics.Average(f => f.NDCG);

    Console.WriteLine($"CV MicroAccuracy: {cvMicro:0.000}");
    Console.WriteLine($"CV MacroAccuracy: {cvMacro:0.000}");
    Console.WriteLine($"Top-K accuracy:   {topK:0.000}");
    Console.WriteLine($"NDCG(10):         {ndcg:0.000}");
    Console.WriteLine($"CV LogLoss:       {cvLogLoss:0.000}");

    Console.WriteLine("----- Training...");

    var trainedModel = estimator.Fit(trainDataView);

    Console.WriteLine("----- Training completed successfully.");

    // map key indices -> label strings taken from the *Label* column
    var outputSchema = trainedModel.GetOutputSchema(trainDataView.Schema);
    var labelCol = outputSchema[nameof(PlateRow.Label)];
    var keyBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    labelCol.GetKeyValues(ref keyBuffer);
    var labels = keyBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();

    return new(trainedModel,
      labels,
      parts.Featurizer,
      estimator);
  }

  public sealed record EstimatorPipelineParts(IEstimator<ITransformer> Featurizer,
    IEstimator<MulticlassPredictionTransformer<MaximumEntropyModelParameters>> Trainer);
}

public sealed record TrainedModel(ITransformer Model,
  string[] Labels,
  IEstimator<ITransformer> Featurizer,
  IEstimator<ITransformer> Estimator);
