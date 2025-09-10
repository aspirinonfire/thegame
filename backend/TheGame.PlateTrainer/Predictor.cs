using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Immutable;

namespace TheGame.PlateTrainer;

public sealed record PlatePrediction
{
  [ColumnName("PredictedLabel")]
  public uint PredictedLabel { get; set; }

  // keep scores if you ever want top-k
  [ColumnName("Score")]
  public float[] Scores { get; set; } = [];
}

public sealed class PredictorFactory(MLContext ml)
{
  public Predictor CreatePredictor(TrainedModel trainedModel) => new(ml, trainedModel);
}

public sealed class Predictor(MLContext ml, TrainedModel trainedModel)
{
  public void Predict(string query, int topK = 5)
  {
    Console.WriteLine($"----- Predictions for \"{query}\":");

    var queryDataView = ml.Data.LoadFromEnumerable([new PlateRow(Label: "", Text: query)]);
    var scoredPredictions = trainedModel.Model.Transform(queryDataView);

    var prediction = ml.Data.CreateEnumerable<PlatePrediction>(scoredPredictions, reuseRowObject: false)
      .First();

    var predictionPairs = trainedModel.Labels.Zip(prediction.Scores, (label, score) => (label, score));

    var top5Matches = predictionPairs
      .OrderByDescending(p => p.score)
      .Take(topK)
      .ToImmutableArray();

    //Console.WriteLine($"Top match: {prediction.PredictedLabel}.");
    foreach (var (label, score) in top5Matches)
    {
      Console.WriteLine($"{label}: {score:P2}");
    }
  }
}
