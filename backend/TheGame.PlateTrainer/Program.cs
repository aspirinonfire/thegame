using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML;
using TheGame.PlateTrainer;

var parsedArgs = TrainerArgParser.ParseCommandArguments(args);

var services = new ServiceCollection()
  .AddSingleton(_ => new MLContext(parsedArgs.Seed))
  .AddSingleton<TrainingDataLoader>()
  .AddSingleton<PipelineFactory>()
  .AddSingleton<ModelEvaluationService>()
  .AddSingleton<ModelTrainingService>()
  .AddSingleton<ModelParamsService>()
  .AddSingleton<ModelParamsService>()
  .AddSingleton<PredictorFactory>()
  .AddSingleton<OnnxModelService>();

await using var sp = services.BuildServiceProvider(new ServiceProviderOptions()
{
  ValidateOnBuild = true,
  ValidateScopes = true
});

var mlDataLoader = sp.GetRequiredService<TrainingDataLoader>();
var predictorFactory = sp.GetRequiredService<PredictorFactory>();

TrainedModel trainedModel = null!;
using (var trainingData = mlDataLoader.ReadTrainingData(parsedArgs.DataPath, parsedArgs.Seed))
{
  trainedModel = parsedArgs.UseSearch ?
    await TrainFromExperiment(sp, trainingData, parsedArgs) :
    await TrainFromParamsFile(sp, trainingData, parsedArgs);
}

var predictor = predictorFactory.CreatePredictor(trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");

static async Task<TrainedModel> TrainFromParamsFile(ServiceProvider serviceProvider,
  TrainingData trainingData,
  TrainerArgs args)
{
  var ml = serviceProvider.GetRequiredService<MLContext>();
  var trainerSvc = serviceProvider.GetRequiredService<ModelTrainingService>();
  var modelParamsSvc = serviceProvider.GetRequiredService<ModelParamsService>();
  var onnxService = serviceProvider.GetRequiredService<OnnxModelService>();

  var modelParams = await modelParamsSvc.ReadModelParamsFromFile(args.HyperParamsPath);

  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: modelParams.TestFraction, seed: modelParams.Seed);

  var trainedModel = trainerSvc.TrainLbfgs(dataSplit, modelParams);

  var currentParams = await modelParamsSvc.ReadModelParamsFromFile(args.HyperParamsPath);
  if (currentParams.ModelMetrics.Ndcg - trainedModel.Metrics.Ndcg > args.NdcgCurrentVsNewThreshold)
  {
    throw new InvalidOperationException("Experiment produced model with NDCG score that is 5% worse!");
  }

  if (string.IsNullOrEmpty(args.OnnxPath))
  {
    Console.WriteLine("ONNX path omitted. Model will not be saved");
  }
  else
  {
    onnxService.ExportToOnnx(trainedModel, dataSplit.TestSet, args.OnnxPath);
  }

  return trainedModel;
}

static async Task<TrainedModel> TrainFromExperiment(ServiceProvider serviceProvider,
  TrainingData trainingData,
  TrainerArgs args)
{
  var ml = serviceProvider.GetRequiredService<MLContext>();
  var trainerSvc = serviceProvider.GetRequiredService<ModelTrainingService>();
  var modelParamsSvc = serviceProvider.GetRequiredService<ModelParamsService>();
  var onnxService = serviceProvider.GetRequiredService<OnnxModelService>();

  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: args.TestFraction, seed: args.Seed);

  var (experiment, estimators) = trainerSvc.CreateMulticlassificationFitExperiment(
    maxModelsToExplore: args.MaxModels,
    dataSplit.TrainSet,
    seed: args.Seed,
    cvFolds: 3);

  var modelFromExperiment = await trainerSvc.RunExperiment(experiment, dataSplit);

  var currentParams = await modelParamsSvc.ReadModelParamsFromFile(args.HyperParamsPath);
  if (currentParams.ModelMetrics.Ndcg - modelFromExperiment.Metrics.Ndcg > args.NdcgCurrentVsNewThreshold)
  {
    throw new InvalidOperationException("Experiment produced model with NDCG score that is 5% worse!");
  }

  var trainingParams = await modelParamsSvc.SaveModelHyperParams(args.HyperParamsPath,
    modelFromExperiment.BestFitParameters!,
    modelFromExperiment.Metrics,
    estimators,
    args.Seed,
    args.TestFraction);

  var trainedModel = trainerSvc.TrainLbfgs(dataSplit, trainingParams);

  if (string.IsNullOrEmpty(args.OnnxPath))
  {
    Console.WriteLine("ONNX path omitted. Model will not be saved");
  }
  else
  {
    onnxService.ExportToOnnx(trainedModel, dataSplit.TestSet, args.OnnxPath);
  }

  return trainedModel;
}

