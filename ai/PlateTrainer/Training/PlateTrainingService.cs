using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;

namespace PlateTrainer.Training;

public sealed class PlateTrainingService(MLContext mlContext, int numOfIterations = 200, float l2Reg = 0.001f)
{
  /// <summary>
  /// See <see href="https://learn.microsoft.com/en-us/azure/machine-learning/algorithm-cheat-sheet?view=azureml-api-1"/>
  /// </summary>
  /// <returns></returns>
  public IEstimator<ITransformer> CreateEstimatorPipeline()
  {
    var (n1gram, n1col) = CreateNgramTransformer(mlContext, "TokenKeys", 1);
    var (n2gram, n2col) = CreateNgramTransformer(mlContext, "TokenKeys", 2);
    var (n3gram, n3col) = CreateNgramTransformer(mlContext, "TokenKeys", 3);
    var (n4gram, n4col) = CreateNgramTransformer(mlContext, "TokenKeys", 4);

    var featurizer = mlContext.Transforms.Text
      .NormalizeText(
        inputColumnName: nameof(PlateTrainingRow.Text),
        outputColumnName: "TextNorm",
        caseMode: TextNormalizingEstimator.CaseMode.Lower,
        keepDiacritics: false,
        keepPunctuations: false,
        keepNumbers: false)
      .Append(mlContext.Transforms.Text.TokenizeIntoWords("RawTokens", "TextNorm"))
      .Append(mlContext.Transforms.Text.RemoveDefaultStopWords(
        inputColumnName: "RawTokens",
        outputColumnName: "CleanTokens",
        language: StopWordsRemovingEstimator.Language.English))
      // text transforms (producengram) works with numeric Id not strings, so we need to convert clean tokens to Ids.
      .Append(mlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: "CleanTokens",
        outputColumnName: "TokenKeys"))
      .Append(n1gram)
      .Append(n2gram)
      .Append(n3gram)
      .Append(n4gram);

    var trainerOpts = new SdcaMaximumEntropyMulticlassTrainer.Options()
    {
      LabelColumnName = nameof(PlateTrainingRow.Label),
      FeatureColumnName = "Features",
      ExampleWeightColumnName = nameof(PlateTrainingRow.Weight),
      MaximumNumberOfIterations = numOfIterations,
      L2Regularization = l2Reg,
      Shuffle = true,
      NumberOfThreads = Environment.ProcessorCount
    };

    return featurizer
      // SDCA trainer expects a single Features column to be trained on.
      // We must concat all ngrams into it so they can be used during training
      .Append(mlContext.Transforms.Concatenate("Features", n1col, n2col, n3col, n4col))
      // trainers work with numeric label Ids not strings.
      .Append(mlContext.Transforms.Conversion.MapValueToKey(nameof(PlateTrainingRow.Label), nameof(PlateTrainingRow.Label)))
      .Append(mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(trainerOpts))
      // trainers produce predicted label as Id not string, convert it to human readable.
      .Append(mlContext.Transforms.Conversion.MapKeyToValue(nameof(PlatePrediction.PredictedLabel), nameof(PlatePrediction.PredictedLabel)));
  }

  public TrainedModel Train(IDataView dataView)
  {
    Console.WriteLine("----- Training...");

    var model = CreateEstimatorPipeline().Fit(dataView);

    Console.WriteLine("----- Training completed successfully.");

    // map key indices -> label strings taken from the *Label* column
    var outputSchema = model.GetOutputSchema(dataView.Schema);
    var labelCol = outputSchema[nameof(PlateTrainingRow.Label)];
    var keyBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    labelCol.GetKeyValues(ref keyBuffer);

    var labels = keyBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();

    return new(model, labels);
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

public sealed record TrainedModel(ITransformer Model, string[] Labels);
