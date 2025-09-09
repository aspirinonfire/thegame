using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using Microsoft.ML.SearchSpace;
using Microsoft.ML.SearchSpace.Option;
using Microsoft.ML.Trainers;
using System.Text.Json;

namespace TheGame.PlateTrainer;

public sealed class ModelTrainingService(MLContext mlContext, PipelineFactory pipelineFactory, ModelEvaluationService modelEvalService)
{
  public TrainedModel TrainLbfgs(DataOperationsCatalog.TrainTestData trainTestData, TrainingParams trainingParams, int cvFolds = 5)
  {
    Console.WriteLine("----- Training from pre-set parameters...");
    var pipelineParams = trainingParams.ModelHyperParams["_pipeline_"];
    Console.WriteLine(JsonSerializer.Serialize(pipelineParams, ModelParamsService.JsonSerializerOptions));

    var featurizerParams = JsonSerializer.Deserialize< NgramFeaturizerParams>(pipelineParams["e0"].ToString(), ModelParamsService.JsonSerializerOptions);
    var lbfgsParams = JsonSerializer.Deserialize<LbfgsMaximumEntropyMulticlassTrainer.Options>(pipelineParams["e1"].ToString(), ModelParamsService.JsonSerializerOptions);

    var estimator = pipelineFactory
      .CreateFeaturizer(featurizerParams!)
      .Append(mlContext.MulticlassClassification.Trainers.LbfgsMaximumEntropy(lbfgsParams));

    Console.WriteLine($"----- Cross Validating ({cvFolds})...");
    
    var folds = mlContext.Data.CrossValidationSplit(trainTestData.TrainSet,
      numberOfFolds: cvFolds,
      seed: trainingParams.Seed);

    var foldMetrics = folds
      .Select(fold =>
      {
        var model = estimator.Fit(fold.TrainSet);
        var scored = model.Transform(fold.TestSet);

        return modelEvalService.CalculateMetricsForSet(scored,
          Enumerable.Range(0, 51).Select(i => $"{i}").ToArray(),
          trainingParams.ModelMetrics.K);
      })
      .ToList();

    SetMetrics.FromAverage(foldMetrics).Print("CV");

    Console.WriteLine("----- Fitting a model...");

    var trainedModel = estimator.Fit(trainTestData.TrainSet);

    Console.WriteLine("----- Training completed successfully.");

    // map key indices -> label strings taken from the *Label* column
    var outputSchema = trainedModel.GetOutputSchema(trainTestData.TrainSet.Schema);
    var labelCol = outputSchema[nameof(PlateRow.Label)];
    var keyBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    labelCol.GetKeyValues(ref keyBuffer);
    var labels = keyBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();

    Console.WriteLine("----- Metrics from params file:");
    trainingParams.ModelMetrics.Print();

    var holdOutMetrics = modelEvalService.EvaluateHoldOutSet(trainedModel, labels, trainTestData.TestSet);

    return new(trainedModel, labels, trainingParams.ModelHyperParams, holdOutMetrics);
  }


  public (AutoMLExperiment, IReadOnlyDictionary<string, SweepableEstimator>) CreateMulticlassificationFitExperiment(int maxModelsToExplore,
    IDataView trainSet,
    int seed,
    int cvFolds,
    double maxRamMb = 1024 * 32)
  {
    Console.WriteLine("Creating experiment...");

    var sweepablePipeline = pipelineFactory.CreateSweepableFeaturizer(
      new SearchSpace<NgramFeaturizerParams>
      {
        [nameof(NgramFeaturizerParams.NgramLength)] = new ChoiceOption(1, 2),

        [nameof(NgramFeaturizerParams.Weighting)] = new ChoiceOption(
          Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.Idf,
          Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.Tf,
          Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.TfIdf),

        [nameof(NgramFeaturizerParams.Binarize)] = new ChoiceOption(true, false)
      })
      .Append(pipelineFactory.CreateSweepableLbfgsEstimator(new SearchSpace<LbfgsMaximumEntropyMulticlassTrainer.Options>()
      {
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.L1Regularization)] = new ChoiceOption(0.0),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.L2Regularization)] = new UniformDoubleOption(0.0001, 1.0, defaultValue: 0.1),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.OptimizationTolerance)] = new UniformDoubleOption(1e-5, 1e-2, defaultValue: 1e-4),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.MaximumNumberOfIterations)] = new ChoiceOption(10_001),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.LabelColumnName)] = new ChoiceOption(nameof(PlateRow.Label)),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.FeatureColumnName)] = new ChoiceOption(PipelineFactory.FeatureColumn),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.InitialWeightsDiameter)] = new ChoiceOption(0.1),

        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.DenseOptimizer)] = new ChoiceOption(true, false),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.HistorySize)] = new ChoiceOption(30),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.StochasticGradientDescentInitilaizationTolerance)] = new ChoiceOption(0.001)
      }));

    var experiment = mlContext.Auto()
      .CreateExperiment(new AutoMLExperiment.AutoMLExperimentSettings()
      {
        Seed = seed
      })
      // pipeline must be set so trial runner can receive proper settings object
      .SetPipeline(sweepablePipeline)
      .SetMaximumMemoryUsageInMegaByte(maxRamMb)
      .SetMaxModelToExplore(maxModelsToExplore)
      //.SetEciCostFrugalTuner()
      .SetSmacTuner(
        numberOfTrees: 30,
        numRandomEISearchConfigurations: 3000,
        // ignore tiny improvements
        epsilon: 0.000001,
        numNeighboursForNumericalParams: 4)
      // The trial runner will contain the training data, and experiment metric configuration
      .SetTrialRunner(new MulticlassRankerTrialRunner(mlContext,
        modelEvalService,
        sweepablePipeline,
        trainSet,
        MulticlassRankerMetric.Ndcg,
        seed,
        cvFolds,
        10));

    mlContext.Log += (o, e) =>
    {
      if (e.Source.Equals("AutoMLExperiment"))
      {
        Console.WriteLine(e.RawMessage);
      }
    };

    return (experiment, sweepablePipeline.Estimators);
  }

  public async Task<TrainedModel> RunExperiment(AutoMLExperiment experiment,
    DataOperationsCatalog.TrainTestData trainTestData)
  {
    Console.WriteLine("Running experiment...");

    var bestFit = await experiment.RunAsync();

    Console.WriteLine("Experiment Best fit params:");
    Console.WriteLine($"Loss: {bestFit.Loss}");
    Console.WriteLine($"Metric: {bestFit.Metric}");
    Console.WriteLine($"Best Trial ID: {bestFit.TrialSettings.TrialId}");
    Console.WriteLine($"Runtime: {bestFit.DurationInMilliseconds / 1000.0:0.00} seconds");

    // 1. Get the schema from the trained model
    var schema = bestFit.Model.GetOutputSchema(trainTestData.TrainSet.Schema);

    // 2. Try to extract key-value annotations (the original string labels)
    VBuffer<ReadOnlyMemory<char>> labelBuffer = default;
    schema[nameof(PlateRow.Label)].GetKeyValues(ref labelBuffer);

    // 3. Convert to string[]
    var labels = labelBuffer
      .DenseValues()
      .Select(l => l.ToString())
      .ToArray();

    var holdOutMetrics = modelEvalService.EvaluateHoldOutSet(bestFit.Model, labels, trainTestData.TestSet);

    return new TrainedModel(bestFit.Model, labels, bestFit.TrialSettings.Parameter, holdOutMetrics);
  }
}

public sealed record TrainedModel(ITransformer Model,
  string[] Labels,
  Parameter? BestFitParameters,
  SetMetrics Metrics);

