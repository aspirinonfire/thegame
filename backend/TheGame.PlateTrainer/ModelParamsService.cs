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

  public static string BumpFileVersion(string currentVersion, string defaultVersion = "0.0.0")
  {
    currentVersion = string.IsNullOrEmpty(currentVersion) ? defaultVersion : currentVersion;

    var lastDot = currentVersion.LastIndexOf(".");
    (var head, var tail) = lastDot < 0 ? ("", currentVersion) : (currentVersion[..lastDot], currentVersion[(lastDot + 1)..]);

    var minorBump = int.Parse(tail) + 1;
    return $"{head}.{minorBump}";
  }

  public async Task<TrainingParams> SaveModelHyperParams(string savePath,
    Parameter modelHyperParams,
    SetMetrics modelMetrics,
    IReadOnlyDictionary<string, SweepableEstimator> estimators,
    int seed,
    double testFraction)
  {
    var currentParams = await ReadModelParamsFromFile(savePath);
    var version = BumpFileVersion(currentParams.Version);

    var trainingParams = new TrainingParams
    (
      Version: version,
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
