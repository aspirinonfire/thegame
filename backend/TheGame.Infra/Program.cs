using Microsoft.Extensions.Configuration;
using Pulumi.Automation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TheGame.Infra;

public static class Program
{
  public static async Task Main(string[] args)
  {
    Console.WriteLine("setting up pulumi program and backend...");
    var gameConfig = GetValidatedInfraConfig();

    var program = PulumiFn.Create(async () => await new TheGameStack(gameConfig).SetupProgram());

    Console.WriteLine("initializing stack...");
    var stackArgs = new InlineProgramArgs(gameConfig.ProjectName, gameConfig.StackName, program)
    {
      EnvironmentVariables = new Dictionary<string, string?>()
      {
        ["PULUMI_CONFIG_PASSPHRASE"] = ""
      }
    };

    // TODO replace it with Azure Blob Storage
    stackArgs.ProjectSettings!.Backend = new ProjectBackend()
    {
      Url = gameConfig.BackendBlobStorageUrl
    };

    var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);
    Console.WriteLine("stack initialized.");

    Console.WriteLine("installing plugins...");
    await stack.Workspace.InstallPluginAsync("azure-native", gameConfig.AzureNativeVersion);
    await stack.Workspace.InstallPluginAsync("azuread", gameConfig.AzureAdVersion);
    Console.WriteLine("azure-native and azuread plugins installed");

    Console.WriteLine("refreshing stack...");
    await stack.RefreshAsync(new RefreshOptions
    {
      OnStandardOutput = Console.WriteLine
    });
    Console.WriteLine("refresh complete.");

    var action = args.FirstOrDefault();
    if (action == "destory")
    {
      Console.WriteLine("destroying stack...");
      await stack.DestroyAsync(new DestroyOptions
      {
        Debug = true,
        LogToStdErr = true,
        OnStandardOutput = Console.WriteLine,
        OnStandardError = Console.WriteLine,
      });
      Console.WriteLine("stack destroy complete.");
    }
    else if (action == "preview")
    {
      var previewResult = await stack.PreviewAsync(new PreviewOptions()
      {
        Debug = true,
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
    else
    {
      Console.WriteLine("updating stack...");
      var result = await stack.UpAsync(new UpOptions
      {
        Debug = true,
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

    Console.WriteLine("Done.");

    Environment.Exit(0);
  }

  private static TheGameConfig GetValidatedInfraConfig()
  {
    var config = new ConfigurationBuilder()
      .AddJsonFile("stack.localdev.json", true)
      .AddEnvironmentVariables()
      .Build();

    var gameConfig = config.Get<TheGameConfig>();

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