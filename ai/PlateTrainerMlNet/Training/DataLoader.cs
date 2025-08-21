using Microsoft.ML;
using Microsoft.ML.Data;
using System.Text.Json;

namespace PlateTrainer.Training;

/// <summary>
/// An intermediate object to control the lifecycle of BinaryReader
/// </summary>
/// <remarks>
/// ML.NET bug:
/// Both <c>LoadFromBinary</c> and <c>ShuffleRows</c> both return <c>IDataView</c>,
/// With <c>LoadFromBinary</c> returning a <c>BinaryReader</c> and <c>ShuffleRows</c> returning a <c>Transformer</c>.
/// <c>BinaryReader</c> needs to be disposed so it closes a file handle correctly, otherwise the file will remain open.
/// </remarks>
/// <param name="dataView"></param>
/// <param name="fileHandle"></param>
/// <param name="disposableDataView"></param>
public sealed class TrainingData(IDataView dataView, IFileHandle fileHandle, IDisposable? disposableDataView) : IDisposable
{
  public IDataView DataView { get; } = dataView;
  private readonly IFileHandle _fileHandle = fileHandle;
  private readonly IDisposable? _disposableDataView = disposableDataView;

  public void Dispose()
  {
    _disposableDataView?.Dispose();
    _fileHandle.Dispose();
  }
}

public sealed class DataLoader(MLContext ml)
{
  private readonly static JsonSerializerOptions _jsonSerializerOpts = new()
  {
    PropertyNameCaseInsensitive = true
  };

  /// <summary>
  /// Read source json, denormalize rows, then save as temp bin file so trainer doesn't need to invoke de-normalization multiple times.
  /// </summary>
  /// <param name="trainingDataPath"></param>
  /// <param name="seed"></param>
  /// <returns></returns>
  public TrainingData ReadTrainingData(string trainingDataPath, int seed)
  {
    Console.WriteLine("----- Preparing training data for training...");

    var trainingRows = ReadTrainingDataAsJsonStream(trainingDataPath);

    var normalizedDataView = ml.Data.LoadFromEnumerable(trainingRows);

    var normalizedBinDataFile = Path.Combine(Path.GetDirectoryName(trainingDataPath)!,
      $"{Path.GetFileNameWithoutExtension(trainingDataPath)}.bin");

    using (var writer = new StreamWriter(normalizedBinDataFile, append: false))
    {
      ml.Data.SaveAsBinary(normalizedDataView, writer.BaseStream);
    }

    var binDataFileHandle = new SimpleFileHandle(ml,
      normalizedBinDataFile,
      false,
      true);

    var preparsedTrainingDataView = ml.Data.LoadFromBinary(new FileHandleSource(binDataFileHandle));

    // Bounded-memory shuffle to break label blocks
    var shuffledDataView = ml.Data.ShuffleRows(input: preparsedTrainingDataView,
     seed: seed,
     shufflePoolSize: 1_000,
     shuffleSource: true);

    return new TrainingData(shuffledDataView, binDataFileHandle, preparsedTrainingDataView as IDisposable);
  }

  /// <summary>
  /// Read source json into async enumerable stream to prevent training data consuming too much memory
  /// </summary>
  /// <param name="trainingDataPath"></param>
  /// <returns></returns>
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
            kvp => (kvp.Value ?? "n/a").Split(","),
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
