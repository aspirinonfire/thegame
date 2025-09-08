using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using Microsoft.ML.SearchSpace;
using Microsoft.ML.SearchSpace.Option;
using Microsoft.ML.Trainers;
using System.Text.Json;

namespace TheGame.PlateTrainer.Training;

public class AutomaticTrainerService(MLContext mlContext, PipelineFactory pipelineFactory)
{
  public SweepableEstimator CreateSweepableFeaturizer()
  {
    // TODO hoist up

    var featuizerSearch = new SearchSpace<NgramFeaturizerParams>
    {
      [nameof(NgramFeaturizerParams.NgramLength)] = new UniformIntOption(min: 1, max: 3, defaultValue: 2),

      [nameof(NgramFeaturizerParams.WeightingIndex)] = new UniformIntOption(min: 0, max: 2, defaultValue: 2)
    };

    return mlContext.Auto().CreateSweepableEstimator(
      factory: (MLContext ctx, NgramFeaturizerParams searchParam) => pipelineFactory.CreateFeaturizer(searchParam),
      ss: featuizerSearch);
  }

  //public SweepableEstimator CreateSweepableLbfgs()
  //{

  //}

  public AutoMLExperiment CreateMulticlassificationFitExperiment(IDataView trainSplit,
    int numOfCvFolds,
    uint trainTimeoutSec,
    double maxRamMb = 1024 * 32)
  {
    Console.WriteLine("Creating experiment...");

    var sweepableFeaturizer = CreateSweepableFeaturizer();

    // note: default algo options and search space will be used if optional params are omitted
    // for default values - see ml.auto package
    var sweepableMulticlass = mlContext.Auto().MultiClassification(
      useLbfgsMaximumEntrophy: true,
      lbfgsMaximumEntrophyOption: new Microsoft.ML.AutoML.CodeGen.LbfgsOption(),
      lbfgsMaximumEntrophySearchSpace: new SearchSpace<Microsoft.ML.AutoML.CodeGen.LbfgsOption>()
      {
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.L2Regularization)] = new UniformDoubleOption(0.0001, 10.0, defaultValue: 1),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.OptimizationTolerance)] = new UniformDoubleOption(1e-5, 1e-2, defaultValue: 1e-4),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.MaximumNumberOfIterations)] = new UniformIntOption(1000, 12000, defaultValue: 2000)
      },

      useFastForest: false,
      useFastTree: false,
      useLbfgsLogisticRegression: false,
      useSdcaLogisticRegression: false,
      useLgbm: false,
      useSdcaMaximumEntrophy: false
      );
      
    var pipeline = sweepableFeaturizer.Append(sweepableMulticlass);

    var experiment = mlContext.Auto()
      .CreateExperiment()
      .SetPipeline(pipeline)
      .SetMaximumMemoryUsageInMegaByte(maxRamMb)
      .SetTrainingTimeInSeconds(trainTimeoutSec)
      .SetDataset(trainSplit, fold: numOfCvFolds)
      // Important! default Label value for metrics is lowercase that may break due to other setup
      .SetMulticlassClassificationMetric(
        MulticlassClassificationMetric.LogLoss,
        labelColumn: nameof(PlateRow.Label));

    return experiment;
  }

  public async Task<TrainedModel> RunExperiment(AutoMLExperiment experiment, IDataView trainSetForLabelExtraction)
  {
    Console.WriteLine("Running experiment...");

    var bestFit = await experiment.RunAsync();
    
    Console.WriteLine("Experiment Best fit params:");
    Console.WriteLine($"Loss: {bestFit.Loss}");
    Console.WriteLine($"Metric: {bestFit.Metric}");

    var paramsString = System.Text.Json.JsonSerializer.Serialize(bestFit.TrialSettings.Parameter, new JsonSerializerOptions
    {
      WriteIndented = true
    });

    Console.WriteLine($"Params:\n{paramsString}");

    // 1. Get the schema from the trained model
    var schema = bestFit.Model.GetOutputSchema(trainSetForLabelExtraction.Schema);

    // 2. Try to extract key-value annotations (the original string labels)
    VBuffer<ReadOnlyMemory<char>> labelBuffer = default;
    schema[nameof(PlateRow.Label)].GetKeyValues(ref labelBuffer);

    // 3. Convert to string[]
    var labels = labelBuffer
      .DenseValues()
      .Select(l => l.ToString())
      .ToArray();

    return new TrainedModel(bestFit.Model, labels);
  }
}
