using Microsoft.ML;
using System.Text.Json.Serialization;

namespace TheGame.PlateTrainer;

public sealed record CvFoldScores()
{
  public uint Label { get; set; }
  public float[] Score { get; set; } = [];
}

public sealed record SetMetrics(double MicroAccuracy,
  double MacroAccuracy,
  double TopKAccuracy,
  double LogLoss,
  double Ndcg,
  int K,
  [property:JsonIgnore] Microsoft.ML.Data.ConfusionMatrix ConfusionMatrix,
  [property: JsonIgnore] IReadOnlyDictionary<string, double> PerClassLogLoss);

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

  public SetMetrics CalculateMetricsForSet(IDataView scored, string[] labels, int k = 10)
  {
    var metrics = ml.MulticlassClassification.Evaluate(scored,
      labelColumnName: "Label",
      topKPredictionCount: k);

    var rows = ml.Data
      .CreateEnumerable<CvFoldScores>(scored,
        reuseRowObject: false,
        ignoreMissingColumns: false);

    var perClassLl = metrics.PerClassLogLoss
      .Select((loss, idx) => new
      {
        loss,
        label = labels[idx]
      })
      .ToDictionary(x => x.label, x => x.loss);

    return new
    (
      metrics.MicroAccuracy,
      metrics.MacroAccuracy,
      metrics.TopKAccuracy,
      metrics.LogLoss,
      Ndcg: CalculateNdcg(rows, k),
      k,
      metrics.ConfusionMatrix,
      PerClassLogLoss: perClassLl.AsReadOnly()
    );
  }

  public SetMetrics EvaluateHoldOutSet(ITransformer model, string[] labels, IDataView holdOutDataView)
  {
    Console.WriteLine("----- Evaluating the trained model...");

    var scored = model.Transform(holdOutDataView);

    var metrics = CalculateMetricsForSet(scored, labels);

    Console.WriteLine($"MacroAccuracy:  {metrics.MacroAccuracy:0.000}");
    Console.WriteLine($"MicroAccuracy:  {metrics.MicroAccuracy:0.000}");
    Console.WriteLine($"Top-K Accuracy: {metrics.TopKAccuracy:0.000}");
    Console.WriteLine($"NDCG(10):       {metrics.Ndcg:0.000}");
    Console.WriteLine($"LogLoss:        {metrics.LogLoss:0.000}");

    var perClassLogLoss = metrics.PerClassLogLoss
      .OrderBy(pcll => pcll.Value);

    Console.WriteLine("Per Class Log Loss:");
    foreach (var classLogLoss in perClassLogLoss)
    {
      Console.WriteLine($"{classLogLoss.Key}: {classLogLoss.Value:0.000}");
    }

    return metrics;

    //Console.WriteLine("Confusion Matrix (rows=actual, cols=predicted):");

    //Console.Write("actual\\pred");
    //foreach (var name in trainedModel.Labels)
    //{
    //  Console.Write($"\t{name}");
    //}
    //Console.WriteLine();

    //for (int actualIndex = 0; actualIndex < metrics.ConfusionMatrix.NumberOfClasses; actualIndex++)
    //{
    //  Console.Write(trainedModel.Labels[actualIndex]);
    //  for (int predictedIndex = 0; predictedIndex < metrics.ConfusionMatrix.NumberOfClasses; predictedIndex++)
    //  {
    //    // Counts are integers represented as doubles; print as integers.
    //    var count = (long)metrics.ConfusionMatrix.Counts[actualIndex][predictedIndex];
    //    Console.Write($"\t{count}");
    //  }
    //  Console.WriteLine();
    //}
  }
}
