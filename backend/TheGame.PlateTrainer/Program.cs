using Microsoft.ML;
using TheGame.PlateTrainer;

// TODO process args or env
const int mlSeed = 123;
const bool useSearch = true;
const double testFraction = 0.2;
const string jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";
const string hyperParamsJsonPath = @"C:\src\thegame\backend\TheGame.PlateTrainer\training_params.json";

var ml = new MLContext(mlSeed);
var mlDataLoader = new TrainingDataLoader(ml);
var pipelineFactory = new PipelineFactory(ml);
var modelValidator = new ModelEvaluationService(ml);
var trainerSvc = new ModelTrainingService(ml, pipelineFactory, modelValidator);

TrainedModel trainedModel = null!;

using (var trainingData = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed))
{
  trainedModel = useSearch ?
    await TrainFromExperiment(trainingData, hyperParamsJsonPath, mlSeed) :
    await TrainFromParamsFile(trainingData, hyperParamsJsonPath);
}

var predictor = new Predictor(ml, trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");

async Task<TrainedModel> TrainFromParamsFile(TrainingData trainingData, string paramsFilePath)
{
  var modelParams = await trainerSvc.ReadModelParamsFromFile(paramsFilePath);

  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: modelParams.TestFraction, seed: modelParams.Seed);

  return trainerSvc.TrainLbfgs(dataSplit, modelParams);
}

async Task<TrainedModel> TrainFromExperiment(TrainingData trainingData, string paramsFilePath, int mlSeed)
{
  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: testFraction, seed: mlSeed);

  var (experiment, estimators) = trainerSvc.CreateMulticlassificationFitExperiment(dataSplit.TrainSet,
    numOfCvFolds: 5,
    maxModelsToExplore: 100,
    seed: mlSeed);

  var model = await trainerSvc.RunExperiment(experiment, dataSplit.TrainSet);

  var trainingParams = await trainerSvc.SaveModelHyperParams(paramsFilePath,
    model.BestFitParameters!,
    model.Metrics,
    estimators,
    mlSeed,
    testFraction);

  return model;
}