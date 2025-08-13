using Microsoft.ML;
using PlateTrainer.Training.Models;

namespace PlateTrainer.Training;

public sealed class DataLoader
{
  public (IDataView dataView, DataViewSchema schema) LoadTrainingData(MLContext ml, string jsonDataPath, int seed)
  {
    Console.WriteLine("----- Loading data...");

    var plates = Array.Empty<PlateTrainingData>();
    using (var dataStream = File.OpenRead(jsonDataPath))
    {
      plates = System.Text.Json.JsonSerializer.Deserialize<PlateTrainingData[]>(dataStream, new System.Text.Json.JsonSerializerOptions()
      {
        PropertyNameCaseInsensitive = true,
      })!;
    }

    var trainingRows = plates
      .Select(plate => new
      {
        plate.Key,
        plate.Weight,
        Descriptions = plate.Description
          .SelectMany(
            kvp => kvp.Value.Split(","),
            (kvp, featureDescription) => $"{featureDescription} {kvp.Key}")
      })
      .SelectMany(
        plate => plate.Descriptions,
        (plate, description) => new PlateTrainingRow(plate.Key, description.Trim(), plate.Weight))
      .ToArray();

    // convert training data json into ml.net consumable data view
    var dataView = ml.Data.LoadFromEnumerable(trainingRows);

    return (dataView, dataView.Schema);
  }
}
