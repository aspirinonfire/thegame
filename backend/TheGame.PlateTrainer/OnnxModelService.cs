using Microsoft.ML;
using Newtonsoft.Json;

namespace TheGame.PlateTrainer;

public class OnnxModelService(MLContext ml)
{
  public void ExportToOnnx(TrainedModel trainedModel,
    IDataView dataForSchemeInference,
    string onnxPath)
  {
    Console.WriteLine("Exporting to ONNX...");

    var probe = ml.Data.TakeRows(dataForSchemeInference, 1);
    var scoredProbe = trainedModel.Model.Transform(probe);

    var keyToValue = ml.Transforms.Conversion
      .MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel")
      .Fit(scoredProbe);

    var exportable = trainedModel.Model.Append(keyToValue);

    using var fs = File.Create(onnxPath);
    ml.Model.ConvertToOnnx(exportable, dataForSchemeInference, fs);

    var onnxOutDir = Path.GetDirectoryName(onnxPath) ?? ".";
    var onnxFilename = Path.GetFileNameWithoutExtension(onnxPath);
    var labelsFilePath = Path.Combine(onnxOutDir, $"{onnxFilename}.labels.json");

    var labelsJson = JsonConvert.SerializeObject(trainedModel.Labels);

    File.WriteAllText(labelsFilePath, labelsJson);
  }
}
