using Microsoft.ML;
using TheGame.PlateTrainer.Prediction;
using TheGame.PlateTrainer.Training;

namespace TheGame.PlateTrainer.Validation;

public sealed record CvFoldScores()
{
  public uint Label { get; set; }
  public float[] Score { get; set; } = [];
}


public sealed class TrainedModelValidationService(MLContext ml)
{
  public static double CalculateNdcg(IEnumerable<CvFoldScores> rows, int k = 10) =>
    rows.Select(r =>
    {
      int labelIndex = (int)r.Label - 1;                     // key is 1-based
      float trueScore = r.Score[labelIndex];
      int rank = 1 + r.Score.Count(s => s > trueScore); // 1 = best
      if (rank > k)
      {
        return 0.0;
      }
      return Math.Log(2.0) / Math.Log(rank + 1.0);        // NDCG@K (natural log like ML.NET)
    })
    .DefaultIfEmpty(0.0)
    .Average();

  public void EvaluateHoldOutSet(TrainedModel trainedModel, IDataView holdOutDataView)
  {
    Console.WriteLine("----- Evaluating the trained model...");

    var scored = trainedModel.Model.Transform(holdOutDataView);

    var metrics = ml.MulticlassClassification.Evaluate(scored, labelColumnName: "Label", topKPredictionCount: 10);

    var rows = ml.Data
      .CreateEnumerable<CvFoldScores>(scored,
        reuseRowObject: false,
        ignoreMissingColumns: false);

    var ndcg = CalculateNdcg(rows, 10);

    Console.WriteLine($"MacroAccuracy:  {metrics.MacroAccuracy:0.000}");
    Console.WriteLine($"MicroAccuracy:  {metrics.MicroAccuracy:0.000}");
    Console.WriteLine($"Top-K Accuracy: {metrics.TopKAccuracy:0.000}");
    Console.WriteLine($"NDCG(10):       {ndcg:0.000}");
    Console.WriteLine($"LogLoss:        {metrics.LogLoss:0.000}");

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
      Console.WriteLine($"{classLogLoss.label}: {classLogLoss.loss:0.000}");
    }
  }
}
