using Microsoft.ML;
using Microsoft.ML.Data;
using System.Collections.Immutable;
using System.Text.Json;

namespace TheGame.PlateTrainer;

public sealed record PlateTrainingData
{
  public string Key { get; set; } = default!;
  public Dictionary<string, string?> Description { get; set; } = [];
}

public sealed record PlateRow(string Label, string Text)
{
  public PlateRow() : this(string.Empty, string.Empty)
  { }
}

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

public sealed class TrainingDataLoader(MLContext ml)
{
  private readonly static IReadOnlyDictionary<string, string[]> _synonymLkp = new Dictionary<string, string[]>()
  {
    { "middle", [ "center" ] },
    { "line", [ "strip", "banner", "stripe" ] },
    { "lines", [ "strips", "banners", "stripes" ] },
    { "solid", [ "all" ] },
    { "plate", [ "background" ] }
  };

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

    return new TrainingData(preparsedTrainingDataView, binDataFileHandle, preparsedTrainingDataView as IDisposable);
  }

  public static IEnumerable<string> CombineAsCarteseanProduct(string[][] parts) => parts
    .Aggregate(
      (IEnumerable<string>)[string.Empty],
      (prefixes, segment) => prefixes.SelectMany(
        prefix => segment,
        (prefix, item) => $"{prefix} {item}".Trim()
      )
    );

  public static IEnumerable<string> CreateFeatureTextCombinations(IDictionary<string, string?> descriptions,
    IReadOnlyDictionary<string, string[]> synonyms)
  {
    return descriptions
      .SelectMany(
        kvp => (kvp.Value ?? "n/a").Split(","),
        (kvp, featureDescription) =>
        {
          var description = featureDescription.Trim();

          var textVariations = description.Split(" ")
            .Select(word =>
            {
              synonyms.TryGetValue(word, out var wordSynonyms);

              string[] wordVariations = [.. wordSynonyms ?? [], word];
              return wordVariations;
            })
            .ToArray();

          var textStrings = CombineAsCarteseanProduct(textVariations);

          synonyms.TryGetValue(kvp.Key, out var featureSynonyms);
          string[] featureVariants = [.. featureSynonyms ?? [], kvp.Key];

          return textStrings
            .Select(text => featureVariants
              .SelectMany(feat => new[] {
                $"{feat} {text}",
                $"{text} {feat}",
              })
              .Concat([text]))
            .SelectMany(expanded => expanded);

        })
      .SelectMany(expanded => expanded);
  }

  /// <summary>
  /// Read source json into async enumerable stream to prevent training data consuming too much memory
  /// </summary>
  /// <param name="trainingDataPath"></param>
  /// <returns></returns>
  private static IEnumerable<PlateRow> ReadTrainingDataAsJsonStream(string trainingDataPath)
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
        if (currentPlateData is null ||
          currentPlateData.Description is null ||
          currentPlateData.Key == "sample")
        {
          continue;
        }

        var descriptions = CreateFeatureTextCombinations(currentPlateData.Description, _synonymLkp);

        foreach (var platePhrase in descriptions)
        {
          yield return new PlateRow
          {
            Label = currentPlateData.Key,
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
