using Microsoft.ML;
using TheGame.PlateTrainer.Prediction;
using TheGame.PlateTrainer.Training;
using TheGame.PlateTrainer.Validation;

// TODO process args or env
const int mlSeed = 123;
const string jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";

var ml = new MLContext(mlSeed);

var mlDataLoader = new DataLoader(ml);
var pipelineFactory = new PipelineFactory(ml);
var trainerSvc = new PlateTrainingService(ml, pipelineFactory);
var autoTrainer = new AutomaticTrainerService(ml, pipelineFactory);
var modelValidator = new TrainedModelValidationService(ml);

TrainedModel trainedModel = null!;

using (var trainingData = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed))
{
  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: 0.1, seed: mlSeed);

  var experiment = autoTrainer.CreateMulticlassificationFitExperiment(dataSplit.TrainSet,
    numOfCvFolds: 3,
    maxModelsToExplore: 300);

  trainedModel = await autoTrainer.RunExperiment(experiment, dataSplit.TrainSet);

  //trainedModel = trainerSvc.Train(dataSplit.TrainSet, mlSeed, numOfIterations: 2000, l2Reg: 0.0001f);

  modelValidator.EvaluateHoldOutSet(trainedModel, dataSplit.TestSet);
}


var predictor = new Predictor(ml, trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");

