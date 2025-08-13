using Microsoft.ML;
using PlateTrainer.Prediction;
using PlateTrainer.Training;
using PlateTrainer.Training.Models;

Console.WriteLine("Initializing training data...");

const int mlSeed = 123;

// Load training data into memory
var jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.sm.json";

var trainerSvc = new PlateTrainingService(seed: mlSeed);
var mlDataLoader = new DataLoader(trainerSvc.MlContext);

var dataView = mlDataLoader.ReadTrainingData(jsonDataPath);

var trainedModel = trainerSvc.Train(dataView);

trainerSvc.EvaluateModel(
  trainedModel,
  [
    new PlateTrainingRow("us-id", "top red white middle blue bottom", 1),
    new PlateTrainingRow("us-ar", "middle diamond", 1),
    new PlateTrainingRow("us-az", "purple bottom", 1),
  ]);

var predictor = new Predictor(trainerSvc.MlContext, trainedModel);

predictor.Predict("top red white middle blue bottom");
predictor.Predict("middle diamond");
predictor.Predict("purple bottom");
predictor.Predict("center bear");
predictor.Predict("solid white plate");

