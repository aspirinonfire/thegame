using Microsoft.ML;
using PlateTrainer.Prediction;
using PlateTrainer.Training;
using PlateTrainer.Validation;

// TODO process args or env
const int mlSeed = 123;
const string jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";
const string trainedModelOnnxPath = @"c:\src\thegame\ui\public\sdca_plates_model.onnx";
const string trainedModelOnnxLabelPath = @"c:\src\thegame\ui\public\sdca_plates_labels.json";

var ml = new MLContext(mlSeed);

var mlDataLoader = new DataLoader(ml);
var trainerSvc = new PlateTrainingService(ml);

TrainedModel trainedModel = null!;
DataViewSchema dataViewSchema = null!;

using (var trainingData = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed))
{
  trainedModel = trainerSvc.Train(trainingData.DataView);
  dataViewSchema = trainingData.DataView.Schema;
}

trainerSvc.ExportToOnnx(trainedModel,
  trainedModelOnnxPath,
  trainedModelOnnxLabelPath);

var predictor = new Predictor(ml, trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");
//predictor.Predict("middle diamond");
//predictor.Predict("purple bottom");
//predictor.Predict("solid white plate red cursive");
//predictor.Predict("yellow plate top dark blue banner");

var modelValidator = new TrainedModelValidationService(ml);
//modelValidator.CalculateFeatureContribution(trainedModel, dataViewSchema, "solid white plate");

modelValidator.EvaluateModel(
  trainedModel,
  [
    new PlateTrainingRow("us-id", "top red white middle blue bottom", 1),
    new PlateTrainingRow("us-ar", "middle diamond", 1),
    new PlateTrainingRow("us-az", "purple bottom", 1),
    new PlateTrainingRow("us-ca", "solid white plate red cursive", 1),
    new PlateTrainingRow("us-ny", "yellow plate top dark blue banner", 1),
  ]);

