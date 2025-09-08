using Microsoft.ML;
using TheGame.PlateTrainer;

// TODO process args or env
const int mlSeed = 123;
const string jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";
const string hyperParamsJsonPath = @"c:\src\thegame\TheGame.PlateTrainer\training_params.json";

var ml = new MLContext(mlSeed);

var mlDataLoader = new TrainingDataLoader(ml);
var pipelineFactory = new PipelineFactory(ml);
var modelValidator = new TrainedModelValidationService(ml);
var trainerSvc = new PlateTrainingService(ml, pipelineFactory, modelValidator);

TrainedModel trainedModel = null!;

using (var trainingData = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed))
{
  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: 0.2, seed: mlSeed);

  var experiment = trainerSvc.CreateMulticlassificationFitExperiment(dataSplit.TrainSet,
    numOfCvFolds: 5,
    maxModelsToExplore: 100);

  trainedModel = await trainerSvc.RunExperiment(experiment, dataSplit.TrainSet);

  await trainerSvc.SaveModelHyperParams(hyperParamsJsonPath, trainedModel.BestFitParameters!, trainedModel.Metrics);

  //trainedModel = trainerSvc.Train(dataSplit.TrainSet, mlSeed, numOfIterations: 2000, l2Reg: 0.0001f);
}

var predictor = new Predictor(ml, trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");

