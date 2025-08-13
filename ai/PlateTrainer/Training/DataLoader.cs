using Microsoft.ML;
using PlateTrainer.Training.Models;
using System.Text.Json;

namespace PlateTrainer.Training;

public sealed class DataLoader(MLContext mlContext)
{
  private readonly static JsonSerializerOptions _jsonSerializerOpts = new()
  {
    PropertyNameCaseInsensitive = true
  };

  public IDataView ReadTrainingData(string trainingDataPath)
  {
    var trainingRows = ReadTrainingDataAsJsonStream(trainingDataPath);

    return mlContext.Data.LoadFromEnumerable(trainingRows);
  }

  private static IEnumerable<PlateTrainingRow> ReadTrainingDataAsJsonStream(string trainingDataPath)
  {
    FileStream? rawTrainingDataStream = null;
    IAsyncEnumerator<PlateTrainingData?>? plateRecordReader = null;

    try
    {
      rawTrainingDataStream = File.OpenRead(trainingDataPath);

      // Async sequence of raw plate records from JSON.
      var plateRecordsAsync = JsonSerializer.DeserializeAsyncEnumerable<PlateTrainingData>(
        rawTrainingDataStream,
        _jsonSerializerOpts);

      // Bridge async JSON to ML.NET's synchronous IDataView consumption.
      plateRecordReader = plateRecordsAsync.GetAsyncEnumerator();
      while (plateRecordReader.MoveNextAsync().AsTask().GetAwaiter().GetResult())
      {
        var currentPlateData = plateRecordReader.Current;
        if (currentPlateData is null || currentPlateData.Description is null)
        {
          continue;
        }

        var descriptions = currentPlateData.Description
          .SelectMany(
            kvp => kvp.Value.Split(","),
            (kvp, featureDescription) => $"{featureDescription.Trim()} {kvp.Key}");

        foreach (var platePhrase in descriptions)
        {
          yield return new PlateTrainingRow
          {
            Label = currentPlateData.Key,
            Weight = currentPlateData.Weight,
            Text = platePhrase
          };
        }
      }
    }
    finally
    {
      plateRecordReader?.DisposeAsync().AsTask().GetAwaiter().GetResult();
      rawTrainingDataStream?.Dispose();
    }
  }
}
