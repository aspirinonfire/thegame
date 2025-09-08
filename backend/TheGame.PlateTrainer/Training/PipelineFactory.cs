using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using Microsoft.ML.Transforms.Text;

namespace TheGame.PlateTrainer.Training;

public sealed record NgramFeaturizerParams()
{
  // We'll map 1..3 directly to ProduceNgrams(ngramLength)
  public int NgramLength { get; set; }

  // We'll map 0,1,2 => Tf, Idf, TfIdf to avoid ChoiceOption ceremony.
  public int WeightingIndex { get; set; }

  // TODO cleanup enum handling
  public NgramExtractingEstimator.WeightingCriteria WeightingEnum => (NgramExtractingEstimator.WeightingCriteria)WeightingIndex;

  public static NgramFeaturizerParams CreateDefault() => new()
  {
    NgramLength = 2,
    WeightingIndex =(int)NgramExtractingEstimator.WeightingCriteria.TfIdf
  };
}

public class PipelineFactory(MLContext mlContext)
{
  public const string CleanTokenColumn = "CleanTokens";
  public const string CleanTokenKeyColumn = "TokenKeys";
  public const string FeaturesColumn = "Features";

  public EstimatorChain<ValueToKeyMappingTransformer> CreateFeaturizer(NgramFeaturizerParams featurizerParams)
  {
    return mlContext.Transforms.Text.TokenizeIntoWords(
        inputColumnName: nameof(PlateRow.Text),
        outputColumnName: CleanTokenColumn,
        separators: [' '])
      // text transforms (producengram) works with numeric Id not strings, so we need to convert clean tokens to Ids.
      .Append(mlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: CleanTokenColumn,
        outputColumnName: CleanTokenKeyColumn))
      .Append(mlContext.Transforms.Text.ProduceNgrams(
        inputColumnName: CleanTokenKeyColumn,
        outputColumnName: FeaturesColumn,
        ngramLength: featurizerParams.NgramLength,
        useAllLengths: true,
        weighting: featurizerParams.WeightingEnum))
      // trainers work with numeric label Ids not strings.
      .Append(mlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: nameof(PlateRow.Label),
        outputColumnName: nameof(PlateRow.Label)));
  }
}
