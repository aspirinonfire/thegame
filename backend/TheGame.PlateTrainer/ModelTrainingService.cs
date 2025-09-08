using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.AutoML.CodeGen;
using Microsoft.ML.Data;
using Microsoft.ML.SearchSpace;
using Microsoft.ML.SearchSpace.Option;
using Microsoft.ML.Trainers;
using System.Text.Json;

namespace TheGame.PlateTrainer;

public sealed class ModelTrainingService(MLContext mlContext, PipelineFactory pipelineFactory, ModelEvaluationService modelValidationService)
{
  private readonly static JsonSerializerOptions _jsonSerializerOptions = new()
  {
    WriteIndented = true,
    PropertyNameCaseInsensitive = true
  };

  public TrainedModel TrainLbfgs(DataOperationsCatalog.TrainTestData trainTestData, TrainingParams trainingParams, int cvFolds = 5)
  {
    Console.WriteLine("----- Training from pre-set parameters...");
    var pipelineParams = trainingParams.ModelHyperParams["_pipeline_"];
    Console.WriteLine(JsonSerializer.Serialize(pipelineParams, _jsonSerializerOptions));

    var pipeline = CreateSweepableFeaturizer([])
      .Append(mlContext.Auto().MultiClassification(
        useLbfgsMaximumEntrophy: true,

        useSdcaMaximumEntrophy: false,
        useLbfgsLogisticRegression: false,
        useSdcaLogisticRegression: false,
        useFastForest: false,
        useFastTree: false,
        useLgbm: false
      ));

    var estimator = pipeline.BuildFromOption(mlContext, pipelineParams);

    Console.WriteLine($"----- Cross Validating ({cvFolds})...");
    
    var folds = mlContext.Data.CrossValidationSplit(trainTestData.TrainSet,
      numberOfFolds: cvFolds,
      seed: trainingParams.Seed);

    var foldMetrics = folds
      .Select(fold =>
      {
        var model = estimator.Fit(fold.TrainSet);
        var scored = model.Transform(fold.TestSet);

        return modelValidationService.CalculateMetricsForSet(scored,
          Enumerable.Range(0, 51).Select(i => $"{i}").ToArray(),
          trainingParams.ModelMetrics.K);
      })
      .ToList();

    var cvMetrics = new SetMetrics(
      MicroAccuracy: foldMetrics.Average(f => f.MicroAccuracy),
      MacroAccuracy: foldMetrics.Average(f => f.MacroAccuracy),
      TopKAccuracy: foldMetrics.Average(f => f.TopKAccuracy),
      LogLoss: foldMetrics.Average(f => f.LogLoss),
      Ndcg: foldMetrics.Average(f => f.Ndcg),
      K: trainingParams.ModelMetrics.K,
      null!,
      null!);


    Console.WriteLine($"CV MicroAccuracy: {cvMetrics.MicroAccuracy:0.000}");
    Console.WriteLine($"CV MacroAccuracy: {cvMetrics.MacroAccuracy:0.000}");
    Console.WriteLine($"Top-{trainingParams.ModelMetrics.K} accuracy: {cvMetrics.TopKAccuracy:0.000}");
    Console.WriteLine($"NDCG({trainingParams.ModelMetrics.K}): {cvMetrics.Ndcg:0.000}");
    Console.WriteLine($"CV LogLoss: {cvMetrics.LogLoss:0.000}");

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
    Console.WriteLine($"MicroAccuracy:  {trainingParams.ModelMetrics.MicroAccuracy:0.000}");
    Console.WriteLine($"MacroAccuracy:  {trainingParams.ModelMetrics.MacroAccuracy:0.000}");
    Console.WriteLine($"Top-K accuracy: {trainingParams.ModelMetrics.TopKAccuracy:0.000}");
    Console.WriteLine($"NDCG:           {trainingParams.ModelMetrics.Ndcg:0.000}");
    Console.WriteLine($"LogLoss:        {trainingParams.ModelMetrics.LogLoss:0.000}");

    var holdOutMetrics = modelValidationService.EvaluateHoldOutSet(trainedModel, labels, trainTestData.TestSet);

    return new(trainedModel, labels, trainingParams.ModelHyperParams, holdOutMetrics);
  }

  public SweepableEstimator CreateSweepableFeaturizer(SearchSpace<NgramFeaturizerParams> featurizerSearchSpace) =>
    mlContext.Auto().CreateSweepableEstimator(
      factory: (ctx, searchParam) => pipelineFactory.CreateFeaturizer(searchParam),
      ss: featurizerSearchSpace);

  public AutoMLExperiment CreateMulticlassificationFitExperiment(IDataView trainSplit,
    int numOfCvFolds,
    int maxModelsToExplore,
    double maxRamMb = 1024 * 32)
  {
    Console.WriteLine("Creating experiment...");

    var featurizerSearchSpace = new SearchSpace<NgramFeaturizerParams>
    {
      [nameof(NgramFeaturizerParams.NgramLength)] = new ChoiceOption(1, 2),

      [nameof(NgramFeaturizerParams.Weighting)] = new ChoiceOption(
        //Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.Idf,
        //Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.Tf,
        Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.TfIdf),

      [nameof(NgramFeaturizerParams.Binarize)] = new ChoiceOption(true, false)
    };

    // note: default algo options and search space will be used if optional params are omitted
    // for default values - see ml.auto package
    var sweepableMulticlass = mlContext.Auto().MultiClassification(
      useLbfgsMaximumEntrophy: true,
      lbfgsMaximumEntrophySearchSpace: new SearchSpace<LbfgsOption>()
      {
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.L1Regularization)] = new ChoiceOption(0.0),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.L2Regularization)] = new UniformDoubleOption(0.0001, 1.0, defaultValue: 0.1),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.OptimizationTolerance)] = new UniformDoubleOption(1e-5, 1e-2, defaultValue: 1e-4),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.MaximumNumberOfIterations)] = new ChoiceOption(10_001),

        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.DenseOptimizer)] = new ChoiceOption(true, false),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.HistorySize)] = new ChoiceOption(30),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.StochasticGradientDescentInitilaizationTolerance)] = new ChoiceOption(0.001)
      },

      useSdcaMaximumEntrophy: false,
      sdcaMaximumEntorphySearchSpace: new SearchSpace<SdcaOption>()
      {
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.L1Regularization)] = new ChoiceOption(0.0),
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.L2Regularization)] = new UniformDoubleOption(0.0001, 10),
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.ConvergenceTolerance)] = new UniformDoubleOption(0.00001, 0.01),
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.MaximumNumberOfIterations)] = new ChoiceOption(10_003),
      },

      useLbfgsLogisticRegression: false,
      useSdcaLogisticRegression: false,
      useFastForest: false,
      useFastTree: false,
      // TOO SLOW for sparse text
      useLgbm: false
    );

    var pipeline = CreateSweepableFeaturizer(featurizerSearchSpace).Append(sweepableMulticlass);
    foreach (var estimator in pipeline.Estimators)
    {
      Console.WriteLine($"{estimator.Key}: {estimator.Value.EstimatorType}");
    }

    var experiment = mlContext.Auto()
      .CreateExperiment()
      .SetPipeline(pipeline)
      .SetMaximumMemoryUsageInMegaByte(maxRamMb)
      .SetMaxModelToExplore(maxModelsToExplore)
      //.SetEciCostFrugalTuner()
      .SetSmacTuner(
        numberOfTrees: 30,
        numRandomEISearchConfigurations: 3000,
        // ignore tiny improvements
        epsilon: 0.00001,
        numNeighboursForNumericalParams: 4)
      .SetDataset(trainSplit, fold: numOfCvFolds)
      // Important! default Label value for metrics is lowercase that may break due to other setup
      .SetMulticlassClassificationMetric(
        MulticlassClassificationMetric.LogLoss,
        labelColumn: nameof(PlateRow.Label));

    mlContext.Log += (o, e) =>
    {
      if (e.Source.Equals("AutoMLExperiment"))
      {
        Console.WriteLine(e.RawMessage);
      }
    };

    return experiment;
  }

  public async Task<TrainedModel> RunExperiment(AutoMLExperiment experiment, IDataView holdOutSet)
  {
    Console.WriteLine("Running experiment...");

    var bestFit = await experiment.RunAsync();

    Console.WriteLine("Experiment Best fit params:");
    Console.WriteLine($"Loss: {bestFit.Loss}");
    Console.WriteLine($"Metric: {bestFit.Metric}");

    // 1. Get the schema from the trained model
    var schema = bestFit.Model.GetOutputSchema(holdOutSet.Schema);

    // 2. Try to extract key-value annotations (the original string labels)
    VBuffer<ReadOnlyMemory<char>> labelBuffer = default;
    schema[nameof(PlateRow.Label)].GetKeyValues(ref labelBuffer);

    // 3. Convert to string[]
    var labels = labelBuffer
      .DenseValues()
      .Select(l => l.ToString())
      .ToArray();

    var holdOutMetrics = modelValidationService.EvaluateHoldOutSet(bestFit.Model, labels, holdOutSet);

    return new TrainedModel(bestFit.Model, labels, bestFit.TrialSettings.Parameter, holdOutMetrics);
  }

  public async Task SaveModelHyperParams(string savePath, Parameter modelHyperParams, SetMetrics modelMetrics, int seed, double testFraction)
  {
    var toSave = JsonSerializer.Serialize(
      new TrainingParams
      (
        Version: "0.0.1",
        Seed: seed,
        TestFraction: testFraction,
        modelHyperParams,
        modelMetrics
      ),
      _jsonSerializerOptions);

    Console.WriteLine($"Saving params:\n{toSave}");


    await File.WriteAllTextAsync(savePath, toSave);
  }

  public async Task<TrainingParams> ReadModelParamsFromFile(string paramsPath)
  {
    var rawFileString = await File.ReadAllTextAsync(paramsPath);

    return JsonSerializer.Deserialize<TrainingParams>(rawFileString, _jsonSerializerOptions)!;
  }
}

public sealed record TrainedModel(ITransformer Model,
  string[] Labels,
  Parameter? BestFitParameters,
  SetMetrics Metrics);

public sealed record TrainingParams(string Version,
  int Seed,
  double TestFraction,
  Parameter ModelHyperParams,
  SetMetrics ModelMetrics);