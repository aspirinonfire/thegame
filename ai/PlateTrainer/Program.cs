using PlateTrainer.Prediction;
using PlateTrainer.Training;
using PlateTrainer.Training.Models;

const int mlSeed = 123;

// Load training data into memory
var jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";

var trainerSvc = new PlateTrainingService(seed: mlSeed);
var mlDataLoader = new DataLoader(trainerSvc.MlContext);

var dataView = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed);

var trainedModel = trainerSvc.Train(dataView);

var predictor = new Predictor(trainerSvc.MlContext, trainedModel);

predictor.Predict("top red white middle blue bottom");
predictor.Predict("middle diamond");
predictor.Predict("purple bottom");
predictor.Predict("solid white plate red cursive");
predictor.Predict("yellow plate top dark blue banner");

trainerSvc.EvaluateModel(
  trainedModel,
  [
    new PlateTrainingRow("us-id", "top red white middle blue bottom", 1),
    new PlateTrainingRow("us-ar", "middle diamond", 1),
    new PlateTrainingRow("us-az", "purple bottom", 1),
    new PlateTrainingRow("us-ca", "solid white plate red cursive", 1),
    new PlateTrainingRow("us-ny", "yellow plate top dark blue banner", 1),
  ]);

trainerSvc.CalculateFeatureContribution(trainedModel, dataView, "solid white plate");
