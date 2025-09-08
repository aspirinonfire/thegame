using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using TheGame.PlateTrainer.Validation;

namespace TheGame.PlateTrainer.Training;

public sealed class PlateTrainingService(MLContext mlContext, PipelineFactory pipelineFactory, int numOfIterations = 200, float l2Reg = 0.001f)
{
  /// <summary>
  /// See <see href="https://learn.microsoft.com/en-us/azure/machine-learning/algorithm-cheat-sheet?view=azureml-api-1"/>
  /// </summary>
  /// <returns></returns>
  public SdcaMaximumEntropyMulticlassTrainer CreateSdcaTrainer()
  {
    return mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(new SdcaMaximumEntropyMulticlassTrainer.Options()
    {
      LabelColumnName = nameof(PlateRow.Label),
      FeatureColumnName = PipelineFactory.FeaturesColumn,
      MaximumNumberOfIterations = numOfIterations,
      L2Regularization = l2Reg,
      Shuffle = true,
      NumberOfThreads = Environment.ProcessorCount
    });
  }

  public LbfgsMaximumEntropyMulticlassTrainer CreateLbfgsTrainer()
  {
    return mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(new LbfgsMaximumEntropyMulticlassTrainer.Options()
    {
      LabelColumnName = nameof(PlateRow.Label),
      FeatureColumnName = PipelineFactory.FeaturesColumn,
      MaximumNumberOfIterations = numOfIterations,
      L2Regularization = l2Reg,
      OptimizationTolerance = 4.515669741338378e-05f
    });
  }

  public TrainedModel Train(IDataView trainDataView, int mlSeed, int cvFolds = 5)
  {
    var featurizer = pipelineFactory.CreateFeaturizer(NgramFeaturizerParams.CreateDefault());

    var trainer = CreateLbfgsTrainer();

    var estimator = featurizer.Append(trainer);

    Console.WriteLine($"----- Cross Validating ({cvFolds})...");
    
    var folds = mlContext.Data.CrossValidationSplit(trainDataView, numberOfFolds: cvFolds, seed: mlSeed);

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

    return new(trainedModel, labels);
  }

  public sealed record EstimatorPipelineParts(IEstimator<ITransformer> Featurizer,
    IEstimator<MulticlassPredictionTransformer<MaximumEntropyModelParameters>> Trainer);
}

public sealed record TrainedModel(ITransformer Model, string[] Labels);
