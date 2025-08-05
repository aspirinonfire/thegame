using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.Text;
using PlateTrainer.Prediction;
using PlateTrainer.Training;
using System.Collections.Immutable;

Console.WriteLine("Initializing training data...");

// Load training data into memory
var jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.sm.json";

var ml = new MLContext(seed: 1);  // TODO use random seed?

var mlDataLoader = new DataLoader();
var pipelineFactory = new PlateModelTrainerPipelineFactory();
var trainer = new Trainer();

var (dataSplit, dataSchema) = mlDataLoader.LoadTrainingData(ml, jsonDataPath);
var pipeline = pipelineFactory.GetMlPipeline(ml);
var trainedModel = trainer.Train(pipeline, dataSplit, dataSchema);

var predictor = new Predictor(ml, trainedModel);

var query = "top red white middle blue bottom";
predictor.Predict(query);

