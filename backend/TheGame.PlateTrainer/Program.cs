using Microsoft.ML;
using TheGame.PlateTrainer;

// TODO process args or env
const int mlSeed = 123;
const bool useSearch = false;
const double testFraction = 0.2;
const int modelsToExplore = 200;
const string jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";
const string hyperParamsJsonPath = @"c:\src\thegame\backend\TheGame.PlateTrainer\training_params.json";
const string onnxPath = @"c:\src\thegame\ui\public\skl_plates_model.onnx";

var ml = new MLContext(mlSeed);
var mlDataLoader = new TrainingDataLoader(ml);
var pipelineFactory = new PipelineFactory(ml);
var modelValidator = new ModelEvaluationService(ml);
var trainerSvc = new ModelTrainingService(ml, pipelineFactory, modelValidator);
var modelParamsSvc = new ModelParamsService();
var onnxService = new OnnxModelService(ml);

TrainedModel trainedModel = null!;

using (var trainingData = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed))
{
  trainedModel = useSearch ?
    await TrainFromExperiment(ml, trainerSvc, modelParamsSvc, trainingData, hyperParamsJsonPath, mlSeed) :
    await TrainFromParamsFile(ml, trainerSvc, modelParamsSvc, onnxService, trainingData, onnxPath, hyperParamsJsonPath);
}

var predictor = new Predictor(ml, trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");

static async Task<TrainedModel> TrainFromParamsFile(MLContext ml,
  ModelTrainingService trainerSvc,
  ModelParamsService modelParamsSvc,
  OnnxModelService onnxService,
  TrainingData trainingData,
  string onnxFilePath,
  string paramsFilePath)
{
  var modelParams = await modelParamsSvc.ReadModelParamsFromFile(paramsFilePath);

  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: modelParams.TestFraction, seed: modelParams.Seed);

  var trainedModel = trainerSvc.TrainLbfgs(dataSplit, modelParams);

  onnxService.ExportToOnnx(trainedModel, dataSplit.TestSet, onnxFilePath);

  return trainedModel;
}

static async Task<TrainedModel> TrainFromExperiment(MLContext ml,
  ModelTrainingService trainerSvc,
  ModelParamsService modelParamsSvc,
  TrainingData trainingData,
  string paramsFilePath,
  int mlSeed)
{
  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: testFraction, seed: mlSeed);

  var (experiment, estimators) = trainerSvc.CreateMulticlassificationFitExperiment(
    maxModelsToExplore: modelsToExplore,
    dataSplit.TrainSet,
    seed: mlSeed,
    cvFolds: 3);

  var model = await trainerSvc.RunExperiment(experiment, dataSplit);

  var trainingParams = await modelParamsSvc.SaveModelHyperParams(paramsFilePath,
    model.BestFitParameters!,
    model.Metrics,
    estimators,
    mlSeed,
    testFraction);

  trainerSvc.TrainLbfgs(dataSplit, trainingParams);

  return model;
}

