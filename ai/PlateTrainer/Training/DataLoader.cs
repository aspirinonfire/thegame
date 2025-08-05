using Microsoft.ML;

namespace PlateTrainer.Training;

public sealed class DataLoader
{
  public (DataOperationsCatalog.TrainTestData dataSplit, DataViewSchema schema) LoadTrainingData(MLContext ml, string jsonDataPath)
  {
    Console.WriteLine("Loading data...");

    var plates = Array.Empty<PlateTrainingData>();
    using (var dataStream = File.OpenRead(jsonDataPath))
    {
      plates = System.Text.Json.JsonSerializer.Deserialize<PlateTrainingData[]>(dataStream, new System.Text.Json.JsonSerializerOptions()
      {
        PropertyNameCaseInsensitive = true,
      })!;
    }

    var trainingRows = plates.SelectMany(plate => plate.Phrases,
      (plate, desc) => new PlateTrainingRow(plate.Key, desc.Text, desc.Weight))
      .ToArray();

    // convert training data json into ml.net consumable data view
    var dataView = ml.Data.LoadFromEnumerable(trainingRows);
    var split = ml.Data.TrainTestSplit(dataView, testFraction: 0.2);

    return (split, dataView.Schema);
  }
}
