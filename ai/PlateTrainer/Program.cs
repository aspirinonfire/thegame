using Microsoft.ML;
using PlateTrainer.Prediction;
using PlateTrainer.Training;

Console.WriteLine("Initializing training data...");

const int mlSeed = 123;

// Load training data into memory
var jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.sm.json";

var ml = new MLContext(seed: mlSeed);

var mlDataLoader = new DataLoader();
var pipelineFactory = new PlateModelTrainerPipelineFactory();
var trainer = new Trainer();

var (dataView, dataSchema) = mlDataLoader.LoadTrainingData(ml, jsonDataPath, mlSeed);
var pipeline = pipelineFactory.GetMlPipeline(ml);

var trainedModel = trainer.Train(pipeline, dataView, dataSchema);

trainer.EvaluateModel(ml, trainedModel);

using var predictor = new Predictor(ml, trainedModel);

predictor.Predict("top red white middle blue bottom");
predictor.Predict("middle diamond");
predictor.Predict("purple bottom");
predictor.Predict("center bear");
predictor.Predict("solid white plate");

