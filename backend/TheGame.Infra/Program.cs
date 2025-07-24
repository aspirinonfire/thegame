using Microsoft.Extensions.Configuration;
using Pulumi.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TheGame.Infra.AppConfiguration;

namespace TheGame.Infra;

public static class Program
{
  public static async Task Main(string[] args)
  {
    // Initialized Pulumi program configuration
    var action = args.FirstOrDefault("preview").ToLowerInvariant();
    Console.WriteLine($"Executing {action}...");
    
    var infraConfig = GetValidatedInfraConfig();
    Console.WriteLine("Config was successfully read and validated.");

    Console.WriteLine($"Setting up Pulumi project {infraConfig.PulumiConfig.ProjectName}...");

    var gameInfraStack = new TheGameStack(infraConfig);
    var program = PulumiFn.Create(gameInfraStack.SetupProgram);

    Console.WriteLine("initializing stack...");
    var stackArgs = new InlineProgramArgs(infraConfig.PulumiConfig.ProjectName, infraConfig.PulumiConfig.StackName, program)
    {
      EnvironmentVariables = new Dictionary<string, string?>()
      {
        ["PULUMI_CONFIG_PASSPHRASE"] = ""
      }
    };

    stackArgs.ProjectSettings!.Backend = new ProjectBackend()
    {
      Url = infraConfig.PulumiConfig.BackendBlobStorageUrl
    };

    var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);

    Console.WriteLine("installing plugins...");
    await stack.Workspace.InstallPluginAsync("azure-native", infraConfig.PulumiConfig.AzureNativeVersion);
    await stack.Workspace.InstallPluginAsync("azuread", infraConfig.PulumiConfig.AzureAdVersion);
    Console.WriteLine("azure-native and azuread plugins installed");

    Console.WriteLine("refreshing stack...");
    await stack.RefreshAsync(new RefreshOptions
    {
      OnStandardOutput = Console.WriteLine
    });
    Console.WriteLine("refresh complete.");

    if (action == "preview")
    {
      Console.WriteLine("Generating preview...");
      var previewResult = await stack.PreviewAsync(new PreviewOptions()
      {
        Debug = false,
        LogToStdErr = true,
        OnStandardOutput = Console.WriteLine,
        OnStandardError = Console.WriteLine,
      });

      if (previewResult.ChangeSummary != null)
      {
        Console.WriteLine("update summary:");
        foreach (var change in previewResult.ChangeSummary)
        {
          Console.WriteLine($"    {change.Key}: {change.Value}");
        }
      }
    }
    else if (action == "deploy")
    {
      // deploy
      Console.WriteLine("Deploying stack updates...");
      var result = await stack.UpAsync(new UpOptions
      {
        Debug = false,
        LogToStdErr = true,
        OnStandardOutput = Console.WriteLine,
        OnStandardError = Console.WriteLine
      });

      if (result.Summary.ResourceChanges != null)
      {
        Console.WriteLine("update summary:");
        foreach (var change in result.Summary.ResourceChanges)
        {
          Console.WriteLine($"    {change.Key}: {change.Value}");
        }
      }
    }
    else if (action == "destroy")
    {
      // deploy
      Console.WriteLine("Destroying stack...");
      var result = await stack.DestroyAsync(new DestroyOptions
      {
        Debug = false,
        LogToStdErr = true,
        OnStandardOutput = Console.WriteLine,
        OnStandardError = Console.WriteLine
      });

      if (result.Summary.ResourceChanges != null)
      {
        Console.WriteLine("destroy summary:");
        foreach (var change in result.Summary.ResourceChanges)
        {
          Console.WriteLine($"    {change.Key}: {change.Value}");
        }
      }
    }
    else
    {
      Console.WriteLine($"Unknown action {action}");
      Environment.Exit(-1);
    }

    Console.WriteLine("Done.");

    Environment.Exit(0);
  }

  private static TheGameInfraConfig GetValidatedInfraConfig()
  {
    var config = new ConfigurationBuilder()
      .AddJsonFile("stack.localdev.json", true)
      .AddEnvironmentVariables()
      .Build();

    var gameConfig = config.Get<TheGameInfraConfig>();
    if (gameConfig is null)
    {
      Console.WriteLine("game stack config is not set or invalid. Please ensure stack.localdev.json exists and is properly configured.");
      Environment.Exit(-1);
    }

    var configValidationErrors = gameConfig.GetValidationErrors();
    if (configValidationErrors.Count > 0)
    {
      var errorMessage = string.Join(Environment.NewLine, configValidationErrors);

      Console.WriteLine($"game stack config is invalid:{Environment.NewLine}{errorMessage}");

      Environment.Exit(-1);
    }

    return gameConfig;
  }
}