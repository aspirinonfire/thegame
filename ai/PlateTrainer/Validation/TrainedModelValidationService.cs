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
    var testerModel = trainedModel.FullTrainedModel.Transform(testDataView);

    var metrics = ml.MulticlassClassification.Evaluate(testerModel, labelColumnName: "LabelKey");

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

    var queryDataView = ml.Data.LoadFromEnumerable(
      [
        new PlateTrainingRow(string.Empty, queryText, 0)
      ]);

    var queryTokensView = trainedModel.Featurizer.Transform(queryDataView);

    var queryTokensBuffer = queryTokensView.GetColumn<VBuffer<float>>(trainedModel.FeatureColumnName).First();
    // indicies of tokens as matched to trainedModel.Tokens
    var queryTokenIndices = queryTokensBuffer.GetIndices().ToArray();
    // specific token values (based on ngram weighting criteria eg Tf-Idf)
    var featurizedTokenValues = queryTokensBuffer.GetValues().ToArray();

    var tokenBuffer = default(VBuffer<ReadOnlyMemory<char>>);
    trainedModel.FullTrainedModel
      .GetOutputSchema(dataViewSchema)["Features"]
      .GetSlotNames(ref tokenBuffer);
    
    var allModelTokens = tokenBuffer.DenseValues()
      .Select(x => x.ToString())
      .ToArray();

    var classBiases = trainedModel.HeadModel
      .GetBiases()
      .ToArray();

    var tokenWeightsByClassIndex = default(VBuffer<float>[]);
    trainedModel.HeadModel.GetWeights(ref tokenWeightsByClassIndex, out var _);

    var activatedTokens = queryTokenIndices
      .Select(tokenIndex => allModelTokens[tokenIndex]);
    Console.WriteLine($"Activated tokens for query \"{queryText}\":\n{string.Join(", ", activatedTokens)}");

    var scores = trainedModel.Labels
      .Select((className, classIndex) =>
      {
        var classTokenWeights = tokenWeightsByClassIndex[classIndex];
        var classBias = classBiases[classIndex];

        var tokenContributions = Enumerable.Range(0, queryTokenIndices.Length)
            .Select(tokenPosition =>
            {
              var tokenIndex = queryTokenIndices[tokenPosition];
              var tokenValue = featurizedTokenValues[tokenPosition];
              var tokenWeight = classTokenWeights.GetItemOrDefault(tokenIndex);

              return new { token = allModelTokens[tokenIndex], rawValue = tokenValue * tokenWeight };
            })
            .OrderByDescending(t => t.rawValue)
            .ToArray();

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
}
