using System.CommandLine;

namespace TheGame.PlateTrainer;

public sealed record TrainerArgs(string DataPath,
  string HyperParamsPath,
  string OnnxPath,
  bool UseSearch,
  int Seed,
  int MaxModels,
  float TestFraction,
  double NdcgCurrentVsNewThreshold);

public static class TrainerArgParser
{
  public static TrainerArgs ParseCommandArguments(string[] args)
  {
    var trainingDataPath = new Option<string>("--data-path")
    {
      Required = true,
      Description = "Path to training data",
    };
    trainingDataPath.Validators.Add(res =>
    {
      var path = res.GetValue(trainingDataPath);
      if (string.IsNullOrEmpty(path))
      {
        res.AddError("Training data path is required!");
        return;
      }

      if (!File.Exists(path))
      {
        res.AddError("Training data path does not exist!");
        return;
      }
    });

    var hyperParamsFile = new Option<string>("--params-path")
    {
      Required = true,
      Description = "Path to model params"
    };
    hyperParamsFile.Validators.Add(res =>
    {
      var path = res.GetValue(hyperParamsFile);
      if (string.IsNullOrEmpty(path))
      {
        res.AddError("Training params file path is required!");
        return;
      }

      if (!File.Exists(path))
      {
        res.AddError("Training params path does not exist!");
        return;
      }
    });

    var onnxPath = new Option<string>("--onnx-path")
    {
      Required = false,
      Description = "Path to output trained model"
    };

    var useSearchFlag = new Option<bool>("--use-search")
    {
      Required = false,
      Description = "Use ML.Net Auto experiment to find best hyperparams",
      DefaultValueFactory = res => false,
    };

    var modelsToExploreCount = new Option<int>("--models")
    {
      Required = false,
      Description = "Number of ML.Net Auto models to build if --use-search specified. Default: 200",
      DefaultValueFactory = res => 200,
    };

    var mlSeedValue = new Option<int>("--seed")
    {
      Required = false,
      Description = "Seed value for any random numbers used during training. Default: 123",
      DefaultValueFactory = res => 123,
    };

    var testFractionValue = new Option<float>("--test-fraction")
    {
      Required = false,
      Description = "Fraction value of how much test data to split for testing purposes only (hold-out). Default: 0.2",
      DefaultValueFactory = res => 0.2f
    };

    var ndcgCurrentVsNewThreshold = new Option<double>("--ndcg-delta-stop")
    {
      Required = false,
      Description = "Max acceptable current trained params vs new model NDCG metrics difference. If new model is worse, trainer will exit with an error. Default: 0.05",
      DefaultValueFactory = res => 0.05
    };

    var rootCommand = new RootCommand("License Plate AI Search trainer")
    {
      trainingDataPath,
      hyperParamsFile,
      onnxPath,
      useSearchFlag,
      modelsToExploreCount,
      mlSeedValue,
      testFractionValue,
      ndcgCurrentVsNewThreshold
    };

    var parsedArgs = rootCommand.Parse(args);

    if (parsedArgs.Errors.Count > 0)
    {
      var errorMessage = string.Join(Environment.NewLine, parsedArgs.Errors);
      throw new InvalidOperationException(errorMessage);
    }

    return new TrainerArgs(
      // required (args are validated so they must be filled by now)
      DataPath: parsedArgs.GetValue(trainingDataPath)!,
      HyperParamsPath: parsedArgs.GetValue(hyperParamsFile)!,
      OnnxPath: parsedArgs.GetValue(onnxPath)!,
      // optional
      UseSearch: parsedArgs.GetValue(useSearchFlag),
      Seed: parsedArgs.GetValue(mlSeedValue),
      MaxModels: parsedArgs.GetValue(modelsToExploreCount),
      TestFraction: parsedArgs.GetValue(testFractionValue),
      NdcgCurrentVsNewThreshold: parsedArgs.GetValue(ndcgCurrentVsNewThreshold)
      );
  }
}
