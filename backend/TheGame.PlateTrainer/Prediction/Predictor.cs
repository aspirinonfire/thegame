using Microsoft.ML;
using System.Collections.Immutable;
using TheGame.PlateTrainer.Training;

namespace TheGame.PlateTrainer.Prediction;

public sealed class Predictor(MLContext ml, TrainedModel trainedModel)
{
  public void Predict(string query, int topK = 5)
  {
    Console.WriteLine($"----- Predictions for \"{query}\":");

    var queryDataView = ml.Data.LoadFromEnumerable([new PlateTrainingRow(Label: "", Text: query)]);
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
