using Microsoft.ML;
using TheGame.PlateTrainer.Training;

namespace TheGame.PlateTrainer.Validation;
public sealed class TrainedModelValidationService(MLContext ml)
{
  public void EvaluateModel(TrainedModel trainedModel, IDataView testDataView)
  {
    Console.WriteLine("----- Evaluating the trained model...");

    var testerModel = trainedModel.Model.Transform(testDataView);

    var metrics = ml.MulticlassClassification.Evaluate(testerModel, labelColumnName: "Label", topKPredictionCount: 10);

    Console.WriteLine($"LogLoss {metrics.LogLoss}");
    Console.WriteLine($"Top-K Accuracy {metrics.TopKAccuracy}");
    Console.WriteLine($"MacroAccuracy {metrics.MacroAccuracy}");
    Console.WriteLine($"MicroAccuracy {metrics.MicroAccuracy}");

    var perClassLogLoss = metrics.PerClassLogLoss
      .Select((loss, idx) => new
      {
        loss,
        label = trainedModel.Labels[idx]
      })
      .OrderBy(pcll => pcll.loss);

    Console.WriteLine("Per Class Log Loss:");
    foreach (var classLogLoss in perClassLogLoss)
    {
      Console.WriteLine($"{classLogLoss.label}: {classLogLoss.loss}");
    }
  }
}
