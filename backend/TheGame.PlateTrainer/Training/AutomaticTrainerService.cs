using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Data;
using Microsoft.ML.SearchSpace;
using Microsoft.ML.SearchSpace.Option;
using Microsoft.ML.Trainers;
using System.Text.Json;
using Microsoft.ML.AutoML.CodeGen;
using Microsoft.ML.Trainers.LightGbm;

namespace TheGame.PlateTrainer.Training;

public class AutomaticTrainerService(MLContext mlContext, PipelineFactory pipelineFactory)
{
  public SweepableEstimator CreateSweepableFeaturizer()
  {
    // TODO hoist up

    var featuizerSearch = new SearchSpace<NgramFeaturizerParams>
    {
      [nameof(NgramFeaturizerParams.NgramLength)] = new ChoiceOption(1, 2),

      [nameof(NgramFeaturizerParams.WeightingIndex)] = new ChoiceOption(2)
    };

    return mlContext.Auto().CreateSweepableEstimator(
      factory: (MLContext ctx, NgramFeaturizerParams searchParam) => pipelineFactory.CreateFeaturizer(searchParam),
      ss: featuizerSearch);
  }

  public AutoMLExperiment CreateMulticlassificationFitExperiment(IDataView trainSplit,
    int numOfCvFolds,
    int maxModelsToExplore,
    double maxRamMb = 1024 * 32)
  {
    Console.WriteLine("Creating experiment...");

    var sweepableFeaturizer = CreateSweepableFeaturizer();

    // note: default algo options and search space will be used if optional params are omitted
    // for default values - see ml.auto package
    var sweepableMulticlass = mlContext.Auto().MultiClassification(
      useLbfgsMaximumEntrophy: true,
      lbfgsLogisticRegressionOption: new LbfgsOption()
      {
        FeatureColumnName = PipelineFactory.FeatureColumn
      },
      lbfgsMaximumEntrophySearchSpace: new SearchSpace<LbfgsOption>()
      {
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.L2Regularization)] = new UniformDoubleOption(0.0001, 1.0, defaultValue: 0.1),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.OptimizationTolerance)] = new UniformDoubleOption(1e-5, 1e-2, defaultValue: 1e-4),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.MaximumNumberOfIterations)] = new ChoiceOption(10_001)
      },

      useLbfgsLogisticRegression: false,
      lbfgsLogisticRegressionSearchSpace: new SearchSpace<LbfgsOption>()
      {
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.L2Regularization)] = new UniformDoubleOption(0.0001, 1.0, defaultValue: 0.1),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.OptimizationTolerance)] = new UniformDoubleOption(1e-5, 1e-2, defaultValue: 1e-4),
        [nameof(LbfgsMaximumEntropyMulticlassTrainer.Options.MaximumNumberOfIterations)] = new ChoiceOption(10_002)
      },

      useSdcaMaximumEntrophy: true,
      sdcaMaximumEntrophyOption: new SdcaOption()
      {
        FeatureColumnName = PipelineFactory.FeatureColumn
      },
      sdcaMaximumEntorphySearchSpace: new SearchSpace<SdcaOption>()
      {
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.L1Regularization)] = new ChoiceOption(0.0),
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.L2Regularization)] = new UniformDoubleOption(0.0001, 10),
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.ConvergenceTolerance)] = new UniformDoubleOption(0.000 - 01, 0.01),
        [nameof(SdcaMaximumEntropyMulticlassTrainer.Options.MaximumNumberOfIterations)] = new ChoiceOption(10_003),
      },

      useSdcaLogisticRegression: false,

      useLgbm: false,
      lgbmOption: new LgbmOption()
      {
        FeatureColumnName = PipelineFactory.FeatureColumn
      },
      lgbmSearchSpace: new SearchSpace<LgbmOption>()
      {
        [nameof(LightGbmMulticlassTrainer.Options.NumberOfIterations)] = new UniformIntOption(100, 5000),
        [nameof(LightGbmMulticlassTrainer.Options.NumberOfLeaves)] = new UniformIntOption(16, 4096),
        [nameof(LightGbmMulticlassTrainer.Options.MinimumExampleCountPerLeaf)] = new UniformIntOption(50, 200),
        [nameof(LightGbmMulticlassTrainer.Options.MaximumBinCountPerFeature)] = new UniformIntOption(32, 255),
        [nameof(LightGbmMulticlassTrainer.Options.LearningRate)] = new UniformDoubleOption(0.001, 1),
        [nameof(LightGbmMulticlassTrainer.Options.UnbalancedSets)] = new ChoiceOption(true, false),
        [nameof(LightGbmMulticlassTrainer.Options.UseSoftmax)] = new ChoiceOption(true),
        [nameof(LightGbmMulticlassTrainer.Options.Sigmoid)] = new UniformDoubleOption(0.1, 1),

        [nameof(LightGbmMulticlassTrainer.Options.Booster.FeatureFraction)] = new UniformDoubleOption(0.1, 1.0),
        [nameof(LightGbmMulticlassTrainer.Options.Booster.SubsampleFraction)] = new UniformDoubleOption(0.1, 1.0),
        [nameof(LightGbmMulticlassTrainer.Options.Booster.L2Regularization)] = new UniformDoubleOption(0.0001, 100.0),
        [nameof(LightGbmMulticlassTrainer.Options.Booster.L1Regularization)] = new ChoiceOption(0.0)
      },

      useFastForest: false,
      useFastTree: false
    );
      
    var pipeline = sweepableFeaturizer.Append(sweepableMulticlass);

    var experiment = mlContext.Auto()
      .CreateExperiment()
      .SetPipeline(pipeline)
      .SetMaximumMemoryUsageInMegaByte(maxRamMb)
      .SetMaxModelToExplore(maxModelsToExplore)
      //.SetEciCostFrugalTuner()
      .SetSmacTuner(
        numberOfTrees: 30,
        numRandomEISearchConfigurations: 3000,
        // ignore tiny improvements
        epsilon: 0.00001,
        numNeighboursForNumericalParams: 4)
      .SetDataset(trainSplit, fold: numOfCvFolds)
      // Important! default Label value for metrics is lowercase that may break due to other setup
      .SetMulticlassClassificationMetric(
        MulticlassClassificationMetric.LogLoss,
        labelColumn: nameof(PlateRow.Label));

    mlContext.Log += (o, e) =>
    {
      if (e.Source.Equals("AutoMLExperiment"))
      {
        Console.WriteLine(e.RawMessage);
      }
    };

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
