using Microsoft.ML;
using TheGame.PlateTrainer;

namespace TheGame.Tests.PlateTrainer;

[Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
public class GamePlateTrainerTests
{
  [Fact]
  public async Task WillTrainModelWithParamsFile()
  {
    var paramsPath = Path.Combine(AppContext.BaseDirectory, "AI_Data", "training_params.json");

    var trainingData = Enumerable.Range(1, 50)
      .Select(i => new PlateRow($"label-{i}", $"text-{i}"))
      .ToArray();

    var ml = new MLContext(42);
    var pipelineFactory = new PipelineFactory(ml);
    var modelValidator = new ModelEvaluationService(ml);
    var trainerSvc = new ModelTrainingService(ml, pipelineFactory, modelValidator);
    var modelParamsSvc = new ModelParamsService();

    var modelParams = await modelParamsSvc.ReadModelParamsFromFile(paramsPath);

    var testData = ml.Data.LoadFromEnumerable(trainingData);

    var dataSplit = ml.Data.TrainTestSplit(testData, testFraction: modelParams.TestFraction, seed: modelParams.Seed);

    var trainedModel = trainerSvc.TrainLbfgs(dataSplit, modelParams);

    Assert.NotNull(trainedModel.Model);
  }
}
