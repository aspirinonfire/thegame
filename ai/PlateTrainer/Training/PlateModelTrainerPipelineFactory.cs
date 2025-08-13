using Microsoft.ML;
using Microsoft.ML.Transforms.Text;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;
using PlateTrainer.Prediction;
using PlateTrainer.Training.Models;
using Microsoft.ML.Trainers;

namespace PlateTrainer.Training;

public sealed record Pipelines(EstimatorChain<ValueToKeyMappingTransformer> Featurizer,
  SdcaMaximumEntropyMulticlassTrainer Trainer,
  EstimatorChain<TransformerChain<KeyToValueMappingTransformer>> FullPipeline);

public sealed class PlateModelTrainerPipelineFactory
{
  public Pipelines GetMlPipeline(MLContext ml)
  {
    Console.WriteLine("----- Creating training pipeline...");

    var (n1gram, n1col) = CreateNgramTransformer(ml, "TokenKeys", 1);
    var (n2gram, n2col) = CreateNgramTransformer(ml, "TokenKeys", 2);
    var (n3gram, n3col) = CreateNgramTransformer(ml, "TokenKeys", 3);
    var (n4gram, n4col) = CreateNgramTransformer(ml, "TokenKeys", 4);

    var featurizer = ml.Transforms.Text
      .NormalizeText(
        inputColumnName: nameof(PlateTrainingRow.Text),
        outputColumnName: "TextNorm",
        caseMode: TextNormalizingEstimator.CaseMode.Lower,
        keepDiacritics: false,
        keepPunctuations: false,
        keepNumbers: false)
      .Append(ml.Transforms.Text.TokenizeIntoWords("RawTokens", "TextNorm"))
      .Append(ml.Transforms.Text.RemoveDefaultStopWords(
        inputColumnName: "RawTokens",
        outputColumnName: "CleanTokens",
        language: StopWordsRemovingEstimator.Language.English))
      .Append(ml.Transforms.Conversion.MapValueToKey(
        inputColumnName: "CleanTokens",
        outputColumnName: "TokenKeys"))
      .Append(n1gram)
      .Append(n2gram)
      .Append(n3gram)
      .Append(n4gram)
      .Append(ml.Transforms.Concatenate("Features", n1col, n2col, n3col, n4col))
      .Append(ml.Transforms.Conversion.MapValueToKey(nameof(PlateTrainingRow.Label), nameof(PlateTrainingRow.Label)));

    var trainer = ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
      labelColumnName: nameof(PlateTrainingRow.Label),
      featureColumnName: "Features",
      exampleWeightColumnName: nameof(PlateTrainingRow.Weight),
      maximumNumberOfIterations: 300,
      l2Regularization: 0.001f);

    var fullPipeline = featurizer
      .Append(ml.Transforms.Conversion.MapValueToKey(nameof(PlateTrainingRow.Label), nameof(PlateTrainingRow.Label))
        .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy(
          labelColumnName: nameof(PlateTrainingRow.Label),
          featureColumnName: "Features",
          exampleWeightColumnName: nameof(PlateTrainingRow.Weight),
          maximumNumberOfIterations: 200,
          l2Regularization: 0.005f
        ))
        .Append(ml.Transforms.Conversion.MapKeyToValue(nameof(PlatePrediction.PredictedLabel), nameof(PlatePrediction.PredictedLabel)))
      );

    return new Pipelines(featurizer, trainer, fullPipeline);
  }

  private static (NgramExtractingEstimator, string) CreateNgramTransformer(MLContext ml, string inputTokenColumnName, int ngramLength)
  {
    var colName = $"W{ngramLength}";

    var ngram = ml.Transforms.Text.ProduceNgrams(
      inputColumnName: inputTokenColumnName,
      outputColumnName: colName,
      ngramLength: ngramLength,
      useAllLengths: false,
      weighting: NgramExtractingEstimator.WeightingCriteria.TfIdf
    );

    return (ngram, colName);
  }
}
