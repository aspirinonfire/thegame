using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using PlateTrainer.Training;

namespace PlateTrainer.Validation;
public sealed class TrainedModelValidationService(MLContext ml)
{
  public void EvaluateModel(TrainedModel trainedModel, IEnumerable<PlateTrainingRow> evalRows)
  {
    Console.WriteLine("----- Evaluating the trained model...");

    var testDataView = ml.Data.LoadFromEnumerable(evalRows);
    var testerModel = trainedModel.Model.Transform(testDataView);

    var metrics = ml.MulticlassClassification.Evaluate(testerModel, labelColumnName: "Label");

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
    DataViewSchema dataViewSchema,
    string queryText)
  {
    Console.WriteLine("----- Calculating feature contributions...");

    var flattenedChains = Flatten(trainedModel.Model);

    var predictor = flattenedChains
      .OfType<MulticlassPredictionTransformer<MaximumEntropyModelParameters>>()
      .Last();

    var maxEntModel = predictor.Model; // weight matrix

    var transformSchema = trainedModel.Model.GetOutputSchema(dataViewSchema);

    var tokenBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    transformSchema["Features"].GetSlotNames(ref tokenBuffer);
    var tokens = tokenBuffer
      .DenseValues()
      .Select(s => s.ToString())
      .ToArray();

    var queryView = ml.Data
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
    Console.WriteLine($"Activated tokens for query \"{queryText}\":\n{string.Join(", ", activatedTokens)}");

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
}
