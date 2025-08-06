using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using Microsoft.ML.Transforms;
using Newtonsoft.Json.Linq;
using PlateTrainer.Training.Models;
using System.Data;

namespace PlateTrainer.Training;

public sealed class Trainer
{
  public TrainedModel Train(MLContext ml,
    EstimatorChain<TransformerChain<KeyToValueMappingTransformer>> pipeline,
    IDataView data,
    DataViewSchema dataSchema)
  {
    Console.WriteLine("----- Training...");
    var trainedModel = pipeline.Fit(data);

    // map key indices -> label strings taken from the *Label* column
    var outputSchema = trainedModel.GetOutputSchema(dataSchema);
    var labelCol = outputSchema[nameof(PlateTrainingRow.Label)];
    var keyBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    labelCol.GetKeyValues(ref keyBuffer);

    var labels = keyBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();

    var sanityRows = new[]
    {
      new PlateTrainingRow("us-ak", "bear center", 1),
      new PlateTrainingRow("us-ar", "center diamond", 1),
      new PlateTrainingRow("us-az", "grand canyon", 1),
      new PlateTrainingRow("us-ca", "all white solid white", 1),
      new PlateTrainingRow("us-id", "famous potatoes", 1),
      new PlateTrainingRow("us-nc", "first flight center", 1),
      new PlateTrainingRow("us-oh", "birthplace aviation", 1),
      new PlateTrainingRow("us-ok", "white dove", 1),
      new PlateTrainingRow("us-or", "green tree", 1),
      new PlateTrainingRow("us-pa", "top blue yellow bottom", 1),
      new PlateTrainingRow("us-ut", "delicate arch", 1),
      new PlateTrainingRow("us-vt", "white box", 1),
      new PlateTrainingRow("us-wa", "blue mountain", 1)
    };

    var sanityData = ml.Data.LoadFromEnumerable(sanityRows);

    var predictions = trainedModel.Transform(sanityData);

    var sanityMetrics = ml.MulticlassClassification.Evaluate(
    predictions,
    labelColumnName: nameof(PlateTrainingRow.Label));

    Console.WriteLine($"Top-1 accuracy on cues: {sanityMetrics.MicroAccuracy:P2}");
    Console.WriteLine($"LogLossReduction:       {sanityMetrics.LogLossReduction:F4}");
    //Console.WriteLine($"{sanityMetrics.ConfusionMatrix.GetFormattedConfusionTable()}");

    return new TrainedModel(trainedModel, labels);
  }

  public void CalculatePfi(MLContext ml,
    TransformerChain<TransformerChain<KeyToValueMappingTransformer>> trainedModel,
    IDataView dataSplit)
  {
    Console.WriteLine("----- Calculating PFI...");

    var transformedData = trainedModel.Transform(dataSplit);

    var pfi = ml.MulticlassClassification.PermutationFeatureImportance(
      trainedModel.LastTransformer,
      transformedData,
      permutationCount: 50);

    // helpful tokens by Log-Loss *reduction*
    var tokenStats = pfi
      //.OrderByDescending(x => x.Value.LogLoss.Mean)
      .Select((feat, i) => new TokenPfiStat
      (
        Token: pfi.Keys.ElementAt(i),
        // positive -> token helps
        // negative -> token hurts
        // near zero -> most likely irrelevant
        Delta: feat.Value.LogLoss.Mean
      ))
      .OrderBy(x => x.Token)
      .ToArray();

    Console.WriteLine("Token stats. Higher (+) more helpful, Higher (-) more hurtful, near 0 - most likely irrelevant.");
    for (int idx = 0; idx < tokenStats.Length; idx++)
    {
      var (Token, Delta) = tokenStats[idx];
      Console.WriteLine($"{idx + 1}. {Token,-25}\t{Delta:P5}");
    }
  }

  public void CalculateFeatureContribution(MLContext ml,
    TransformerChain<TransformerChain<KeyToValueMappingTransformer>> trainedModel,
    IDataView data,
    string queryText)
  {
    Console.WriteLine("----- Calculating feature contributions...");

    var flattenedChains = Flatten(trainedModel);

    var predictor = flattenedChains
      .OfType<MulticlassPredictionTransformer<MaximumEntropyModelParameters>>()
      .Last();

    var maxEntModel = predictor.Model; // weight matrix

    var transformSchema = trainedModel.Transform(data).Schema;

    var tokenBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    transformSchema["Features"].GetSlotNames(ref tokenBuffer);
    var tokens = tokenBuffer
      .DenseValues()
      .Select(s => s.ToString())
      .ToArray();

    var labelBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    transformSchema["Label"].GetKeyValues(ref labelBuffer);
    var labels = labelBuffer
      .DenseValues()
      .Select(s => s.ToString())
      .ToArray();

    var queryView = ml.Data
      .LoadFromEnumerable<PlateTrainingRow>(
        [new ("?", queryText, 1)]);

    var featureVector = trainedModel
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

    for (int classIndex = 0; classIndex < labels.Length; classIndex++)
    {
      var flatWeights = weights[classIndex];

      var contrib = Enumerable.Range(0, featureSlotIndices.Length)
          .Select(pos =>
          {
            var slot = featureSlotIndices[pos];
            var val = featureSlotValues[pos];
            var w = flatWeights.GetItemOrDefault(slot);   // exact weight for this slot
            var bias = biasVal[classIndex];
            
            return new { token = tokens[slot], rawValue = val * w, bias, biasedValue = val * w + bias };
          })
          .OrderByDescending(t => t.biasedValue)
          .ToArray();

      Console.WriteLine($"=== {labels[classIndex]} ===");
      foreach (var t in contrib)
      {
        Console.WriteLine($"{t.token,-18} biased: {t.biasedValue}");
      }
    }
  }

  private sealed record TokenPfiStat(string Token, double Delta);

  static IEnumerable<ITransformer> Flatten(ITransformer root)
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
}
