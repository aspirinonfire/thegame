using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using System.Text.Json.Serialization;

namespace TheGame.PlateTrainer;

public sealed record NgramFeaturizerParams()
{
  public int NgramLength { get; set; }

  [JsonConverter(typeof(JsonStringEnumConverter))]
  public Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria Weighting { get; set; }

  /// <summary>
  /// Use 1/0 instead of TF/IDF
  /// </summary>
  public bool Binarize { get; set; }

  public static NgramFeaturizerParams CreateDefault() => new()
  {
    NgramLength = 2,
    Weighting = Microsoft.ML.Transforms.Text.NgramExtractingEstimator.WeightingCriteria.TfIdf
  };
}

public class PipelineFactory(MLContext mlContext)
{
  public const string CleanTokenColumn = "CleanTokens";
  public const string CleanTokenKeyColumn = "TokenKeys";
  public const string FeatureColumn = "Features";

  public EstimatorChain<ValueToKeyMappingTransformer> CreateFeaturizer(NgramFeaturizerParams featurizerParams)
  {
    IEstimator<ITransformer> baseChain = mlContext.Transforms.Text.TokenizeIntoWords(
        inputColumnName: nameof(PlateRow.Text),
        outputColumnName: CleanTokenColumn,
        separators: [' '])
      // text transforms (producengram) works with numeric Id not strings, so we need to convert clean tokens to Ids.
      .Append(mlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: CleanTokenColumn,
        outputColumnName: CleanTokenKeyColumn,
        keyOrdinality: ValueToKeyMappingEstimator.KeyOrdinality.ByValue,
        addKeyValueAnnotationsAsText: true));

    if (featurizerParams.Binarize)
    {
      baseChain = baseChain
        .Append(mlContext.Transforms.Text.ProduceNgrams(
          inputColumnName: CleanTokenKeyColumn,
          outputColumnName: "ngram_col",
          ngramLength: featurizerParams.NgramLength,
          useAllLengths: true,
          weighting: featurizerParams.Weighting))
        // binarize in place: non-zero -> true, then cast back to 0/1 float
        .Append(mlContext.Transforms.Conversion.ConvertType(
          inputColumnName: "ngram_col",
          outputColumnName: "bool_bin",
          outputKind: DataKind.Boolean))
        .Append(mlContext.Transforms.Conversion.ConvertType(
          inputColumnName: "bool_bin",
          outputColumnName: FeatureColumn,
          outputKind: DataKind.Single));
    }
    else
    {
      baseChain = baseChain
        .Append(mlContext.Transforms.Text.ProduceNgrams(
          inputColumnName: CleanTokenKeyColumn,
          outputColumnName: FeatureColumn,
          ngramLength: featurizerParams.NgramLength,
          useAllLengths: true,
          weighting: featurizerParams.Weighting));
    }
      
    // trainers work with numeric label Ids not strings.
    return baseChain.Append(mlContext.Transforms.Conversion.MapValueToKey(
      inputColumnName: nameof(PlateRow.Label),
      outputColumnName: nameof(PlateRow.Label),
      keyOrdinality: ValueToKeyMappingEstimator.KeyOrdinality.ByValue,
      addKeyValueAnnotationsAsText: true));
  }
}
