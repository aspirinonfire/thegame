using Microsoft.ML.AutoML;
using Microsoft.ML.SearchSpace;
using System.Text.Json;

namespace TheGame.PlateTrainer;

public sealed record TrainingParams(string Version,
  int Seed,
  double TestFraction,
  KeyValuePair<string, string>[] Estimators,
  Parameter ModelHyperParams,
  SetMetrics ModelMetrics);

public sealed class ModelParamsService
{
  public readonly static JsonSerializerOptions JsonSerializerOptions = new()
  {
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    IncludeFields = true
  };

  public async Task<TrainingParams> SaveModelHyperParams(string savePath,
    Parameter modelHyperParams,
    SetMetrics modelMetrics,
    IReadOnlyDictionary<string, SweepableEstimator> estimators,
    int seed,
    double testFraction)
  {
    var trainingParams = new TrainingParams
    (
      Version: "0.0.1",
      Seed: seed,
      TestFraction: testFraction,
      Estimators: estimators
        .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value?.ToString() ?? "n/a"))
        .ToArray(),
      modelHyperParams,
      modelMetrics
    );

    var toSave = JsonSerializer.Serialize(trainingParams, JsonSerializerOptions);

    Console.WriteLine($"Saving params:\n{toSave}");

    await File.WriteAllTextAsync(savePath, toSave);

    return trainingParams;
  }

  public async Task<TrainingParams> ReadModelParamsFromFile(string paramsPath)
  {
    var rawFileString = await File.ReadAllTextAsync(paramsPath);

    return JsonSerializer.Deserialize<TrainingParams>(rawFileString, JsonSerializerOptions)!;
  }
}
