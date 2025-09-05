using Microsoft.ML;
using TheGame.PlateTrainer.Prediction;
using TheGame.PlateTrainer.Validation;
using TheGame.PlateTrainer.Training;

// TODO process args or env
const int mlSeed = 123;
const string jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";

var ml = new MLContext(mlSeed);

var mlDataLoader = new DataLoader(ml);
var trainerSvc = new PlateTrainingService(ml);

TrainedModel trainedModel = null!;

using (var trainingData = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed))
{
  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: 0.1, seed: mlSeed);

  trainedModel = trainerSvc.Train(dataSplit.TrainSet);

  var modelValidator = new TrainedModelValidationService(ml);
  modelValidator.EvaluateModel(trainedModel, dataSplit.TestSet);
}


var predictor = new Predictor(ml, trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");

