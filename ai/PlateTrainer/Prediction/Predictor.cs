using Microsoft.ML;
using PlateTrainer.Training.Models;
using System.Collections.Immutable;

namespace PlateTrainer.Prediction;

public sealed class Predictor(MLContext ml, TrainedModel trainedModel) : IDisposable
{
  private readonly PredictionEngine<PlateQuery, PlatePrediction> _predictionEngine =
    ml.Model.CreatePredictionEngine<PlateQuery, PlatePrediction>(trainedModel.Model);

  public void Predict(string query)
  {
    Console.WriteLine("----- Predicting...");

    var prediction = _predictionEngine.Predict(new PlateQuery(query));

    var predictionPairs = trainedModel.Labels.Zip(prediction.Scores, (label, score) => (label, score));

    var top5Matches = predictionPairs
      .OrderByDescending(p => p.score)
      .Take(5)
      .ToImmutableArray();

    Console.WriteLine($"Query: \"{query}\"");
    //Console.WriteLine($"Top match: {prediction.PredictedLabel}.");
    foreach (var (label, score) in top5Matches)
    {
      Console.WriteLine($"{label}: {score:P2}");
    }
  }

  public void Dispose()
  {
    _predictionEngine.Dispose();
  }
}
