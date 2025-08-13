using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms.Text;
using PlateTrainer.Prediction;
using PlateTrainer.Training.Models;

namespace PlateTrainer.Training;

public sealed class PlateTrainingService(int seed, int numOfIterations = 200, float l2Reg = 0.001f)
{
  public MLContext MlContext { get; } = new MLContext(seed);

  public IEstimator<ITransformer> CreateEstimatorPipeline()
  {
    var (n1gram, n1col) = CreateNgramTransformer(MlContext, "TokenKeys", 1);
    var (n2gram, n2col) = CreateNgramTransformer(MlContext, "TokenKeys", 2);
    var (n3gram, n3col) = CreateNgramTransformer(MlContext, "TokenKeys", 3);
    var (n4gram, n4col) = CreateNgramTransformer(MlContext, "TokenKeys", 4);

    var featurizer = MlContext.Transforms.Text
      .NormalizeText(
        inputColumnName: nameof(PlateTrainingRow.Text),
        outputColumnName: "TextNorm",
        caseMode: TextNormalizingEstimator.CaseMode.Lower,
        keepDiacritics: false,
        keepPunctuations: false,
        keepNumbers: false)
      .Append(MlContext.Transforms.Text.TokenizeIntoWords("RawTokens", "TextNorm"))
      .Append(MlContext.Transforms.Text.RemoveDefaultStopWords(
        inputColumnName: "RawTokens",
        outputColumnName: "CleanTokens",
        language: StopWordsRemovingEstimator.Language.English))
      .Append(MlContext.Transforms.Conversion.MapValueToKey(
        inputColumnName: "CleanTokens",
        outputColumnName: "TokenKeys"))
      .Append(n1gram)
      .Append(n2gram)
      .Append(n3gram)
      .Append(n4gram)
      .Append(MlContext.Transforms.Concatenate("Features", n1col, n2col, n3col, n4col))
      .Append(MlContext.Transforms.Conversion.MapValueToKey(nameof(PlateTrainingRow.Label), nameof(PlateTrainingRow.Label)));

    var sdcaMaxEntTrainer = MlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
      labelColumnName: nameof(PlateTrainingRow.Label),
      featureColumnName: "Features",
      exampleWeightColumnName: nameof(PlateTrainingRow.Weight),
      maximumNumberOfIterations: numOfIterations,
      l2Regularization: l2Reg);

    return featurizer
      .Append(MlContext.Transforms.Conversion.MapValueToKey(nameof(PlateTrainingRow.Label), nameof(PlateTrainingRow.Label))
        .Append(sdcaMaxEntTrainer)
        .Append(MlContext.Transforms.Conversion.MapKeyToValue(nameof(PlatePrediction.PredictedLabel), nameof(PlatePrediction.PredictedLabel)))
      );
  }

  public TrainedModel Train(IDataView dataView)
  {
    var model = CreateEstimatorPipeline().Fit(dataView);

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

  public void EvaluateModel(TrainedModel trainedModel, IEnumerable<PlateTrainingRow> sanityCheck)
  {
    var testDataView = MlContext.Data.LoadFromEnumerable(sanityCheck);
    var testerModel = trainedModel.Model.Transform(testDataView);

    var metrics = MlContext.MulticlassClassification.Evaluate(testerModel, labelColumnName: "Label");

    Console.WriteLine($"LogLoss {metrics.LogLoss}");
    Console.WriteLine($"LogLossReduction {metrics.LogLossReduction}");
    Console.WriteLine($"MacroAccuracy {metrics.MacroAccuracy}");
    Console.WriteLine($"MicroAccuracy {metrics.MicroAccuracy}");

    var perClassLogLoss = metrics.PerClassLogLoss.Select((loss, idx) => new
    {
      loss,
      label = trainedModel.Labels[idx]
    });

    Console.WriteLine("Per Class Log Loss:");
    foreach (var classLogLoss in perClassLogLoss)
    {
      Console.WriteLine($"{classLogLoss.label}: {classLogLoss.loss}");
    }
  }

  public void CalculateFeatureContribution(TrainedModel trainedModel,
    IDataView data,
    string queryText)
  {
    Console.WriteLine("----- Calculating feature contributions...");

    var flattenedChains = Flatten(trainedModel.Model);

    var predictor = flattenedChains
      .OfType<MulticlassPredictionTransformer<MaximumEntropyModelParameters>>()
      .Last();

    var maxEntModel = predictor.Model; // weight matrix

    var transformSchema = trainedModel.Model.Transform(data).Schema;

    var tokenBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    transformSchema["Features"].GetSlotNames(ref tokenBuffer);
    var tokens = tokenBuffer
      .DenseValues()
      .Select(s => s.ToString())
      .ToArray();

    var queryView = MlContext.Data
      .LoadFromEnumerable<PlateTrainingRow>(
        [new("?", queryText, 1)]);

    var featureVector = trainedModel.Model
      .Transform(queryView)
      .GetColumn<VBuffer<float>>("Features")
      .First();

    var featureSlotIndices = featureVector.GetIndices().ToArray();
    var featureSlotValues = featureVector.GetValues().ToArray();

    var weights = default(VBuffer<float>[]);
    maxEntModel.GetWeights(ref weights, out var classes);

    var activatedTokens = featureSlotIndices
      .Select(featureIndex => tokens[featureIndex]);
    Console.WriteLine($"Activated tokens for query:\n{string.Join(", ", activatedTokens)}");

    var biasVal = maxEntModel.GetBiases().ToArray();

    var scores = trainedModel.Labels
      .Select((className, classIndex) =>
      {
        var flatWeights = weights[classIndex];

        var tokenContributions = Enumerable.Range(0, featureSlotIndices.Length)
            .Select(pos =>
            {
              var slot = featureSlotIndices[pos];
              var val = featureSlotValues[pos];
              var w = flatWeights.GetItemOrDefault(slot);   // exact weight for this slot

              return new { token = tokens[slot], rawValue = val * w };
            })
            .OrderByDescending(t => t.rawValue)
            .ToArray();

        var classBias = biasVal[classIndex];
        var classScore = tokenContributions.Sum(c => c.rawValue) + classBias;

        return new
        {
          className,
          classScore,
          classBias,
          tokenContributions
        };
      })
      .OrderByDescending(x => x.classScore)
      .ToArray();

    foreach (var classContribs in scores)
    {
      Console.WriteLine($"=== {classContribs.className}: (Token sum: {classContribs.classScore}, bias: {classContribs.classBias}) ===");
      foreach (var token in classContribs.tokenContributions)
      {
        Console.WriteLine($"{token.token,-18} token Value: {token.rawValue}");
      }

    }
  }

  private static IEnumerable<ITransformer> Flatten(ITransformer root)
  {
    // yield the root itself
    yield return root;

    // does the object’s raw type start with "TransformerChain`1" ?
    var t = root.GetType();
    if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(TransformerChain<>))
    {
      // cast via non-generic IEnumerable to avoid type-mismatch
      foreach (var child in (System.Collections.IEnumerable)root)
      {
        foreach (var grand in Flatten((ITransformer)child))
        {
          yield return grand;
        }
      }
    }
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
