using Google.Protobuf;
using Microsoft.ML;
using System.IO;
using TheGame.PlateTrainer;

// TODO process args or env
const int mlSeed = 123;
const bool useSearch = false;
const double testFraction = 0.2;
const int modelsToExplore = 100;
const string jsonDataPath = @"c:\src\thegame\ai\training_data\plate_descriptions.json";
const string hyperParamsJsonPath = @"c:\src\thegame\backend\TheGame.PlateTrainer\training_params.json";
const string onnxPath = @"c:\src\thegame\ui\public\skl_plates_model.onnx";

var ml = new MLContext(mlSeed);
var mlDataLoader = new TrainingDataLoader(ml);
var pipelineFactory = new PipelineFactory(ml);
var modelValidator = new ModelEvaluationService(ml);
var trainerSvc = new ModelTrainingService(ml, pipelineFactory, modelValidator);

TrainedModel trainedModel = null!;

using (var trainingData = mlDataLoader.ReadTrainingData(jsonDataPath, mlSeed))
{
  trainedModel = useSearch ?
    await TrainFromExperiment(ml, trainerSvc, trainingData, hyperParamsJsonPath, mlSeed) :
    await TrainFromParamsFile(ml, trainerSvc, trainingData, hyperParamsJsonPath);
}

var predictor = new Predictor(ml, trainedModel);

predictor.Predict("solid white plate");

predictor.Predict("top red white middle blue bottom");

static async Task<TrainedModel> TrainFromParamsFile(MLContext ml, ModelTrainingService trainerSvc, TrainingData trainingData, string paramsFilePath)
{
  var modelParams = await trainerSvc.ReadModelParamsFromFile(paramsFilePath);

  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: modelParams.TestFraction, seed: modelParams.Seed);

  var trainedModel = trainerSvc.TrainLbfgs(dataSplit, modelParams);

  Console.WriteLine("Exporting to ONNX...");

  var probe = ml.Data.TakeRows(dataSplit.TrainSet, 1);
  var scoredProbe = trainedModel.Model.Transform(probe);

  var keyToValue = ml.Transforms.Conversion
    .MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel")
    .Fit(scoredProbe);

  var exportable = trainedModel.Model.Append(keyToValue);

  using var fs = File.Create(onnxPath);
  ml.Model.ConvertToOnnx(exportable, dataSplit.TestSet, fs);

  return trainedModel;
}

static async Task<TrainedModel> TrainFromExperiment(MLContext ml, ModelTrainingService trainerSvc, TrainingData trainingData, string paramsFilePath, int mlSeed)
{
  var dataSplit = ml.Data.TrainTestSplit(trainingData.DataView, testFraction: testFraction, seed: mlSeed);

  var (experiment, estimators) = trainerSvc.CreateMulticlassificationFitExperiment(
    maxModelsToExplore: modelsToExplore,
    seed: mlSeed);

  var model = await trainerSvc.RunExperiment(experiment, dataSplit, cvFolds: 10);

  var trainingParams = await trainerSvc.SaveModelHyperParams(paramsFilePath,
    model.BestFitParameters!,
    model.Metrics,
    estimators,
    mlSeed,
    testFraction);

  trainerSvc.TrainLbfgs(dataSplit, trainingParams);

  return model;
}

//static void EmbedLabelsAsOutput(string onnxPath, IReadOnlyList<string> labels)
//{
//  var model = ModelProto.Parser.ParseFrom(File.ReadAllBytes(onnxPath));
//  var g = model.Graph;

//  // Build a string tensor with class names
//  var lblTensor = new TensorProto
//  {
//    Name = "ClassLabels.const",
//    DataType = (int)TensorProto.Types.DataType.String
//  };
//  lblTensor.Dims.Add(labels.Count);
//  // string_data is repeated bytes
//  lblTensor.StringData.Add(labels.Select(s => ByteString.CopyFromUtf8(s)));

//  // Constant node producing the labels
//  var constNode = new NodeProto
//  {
//    Name = "ClassLabels_Const",
//    OpType = "Constant",
//  };
//  constNode.Output.Add("ClassLabels.output");
//  constNode.Attribute.Add(new AttributeProto
//  {
//    Name = "value",
//    Type = AttributeProto.Types.AttributeType.Tensor,
//    T = lblTensor
//  });
//  g.Node.Add(constNode);

//  // Declare graph output for the labels
//  var outInfo = new ValueInfoProto { Name = "ClassLabels.output" };
//  outInfo.Type = new TypeProto
//  {
//    TensorType = new TypeProto.Types.Tensor
//    {
//      ElemType = (int)TensorProto.Types.DataType.String,
//      Shape = new TensorShapeProto { Dim = { new TensorShapeProto.Types.Dimension { DimValue = labels.Count } } }
//    }
//  };
//  g.Output.Add(outInfo);

//  File.WriteAllBytes(onnxPath, model.ToByteArray());
//}