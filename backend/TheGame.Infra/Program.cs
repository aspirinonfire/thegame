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
    var config = new ConfigurationBuilder()
      .AddJsonFile("stack.localdev.json", true)
      .AddEnvironmentVariables()
      .Build();

    //Environment.SetEnvironmentVariable("PULUMI_CONFIG_PASSPHRASE_FILE", string.Empty);
    //Environment.SetEnvironmentVariable("PULUMI_CONFIG_PASSPHRASE", string.Empty);

    var gameConfig = config.Get<TheGameConfig>();

    var configValidationErrors = gameConfig.GetValidationErrors();
    if (configValidationErrors.Count > 0)
    {
      var errorMessage = string.Join(Environment.NewLine, configValidationErrors);

      Console.WriteLine($"game stack config is invalid:{Environment.NewLine}{errorMessage}");

      Environment.Exit(-1);
    }

    var program = PulumiFn.Create(() => new TheGameStack(gameConfig));

    var stackArgs = new InlineProgramArgs(gameConfig.ProjectName, gameConfig.StackName, program)
    {
      EnvironmentVariables = new Dictionary<string, string?>()
      {
        ["PULUMI_CONFIG_PASSPHRASE"] = ""
      }
    };

    stackArgs.ProjectSettings!.Backend = new ProjectBackend()
    {
      Url = "file://."
    };

    var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);
    Console.WriteLine("stack initialized.");
    
    //await stack.Workspace.InstallPluginAsync("azure-native", gameConfig.AzureNativeVersion);
    //Console.WriteLine("azure native plugin installed");

    Console.WriteLine("refreshing stack...");
    await stack.RefreshAsync(new RefreshOptions
    {
      OnStandardOutput = Console.WriteLine
    });
    Console.WriteLine("refresh complete");

    // to destroy our program, we can run "dotnet run destroy"
    var isDestroy = args.Any() && args[0] == "destroy";
    if (isDestroy)
    {
      Console.WriteLine("destroying stack...");
      await stack.DestroyAsync(new DestroyOptions { OnStandardOutput = Console.WriteLine });
      Console.WriteLine("stack destroy complete");
    }
    else
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
          Console.WriteLine($"    {change.Key}: {change.Value}");
      }

      //Console.WriteLine("updating stack...");
      //var result = await stack.UpAsync(new UpOptions { OnStandardOutput = Console.WriteLine });

      //if (result.Summary.ResourceChanges != null)
      //{
      //  Console.WriteLine("update summary:");
      //  foreach (var change in result.Summary.ResourceChanges)
      //    Console.WriteLine($"    {change.Key}: {change.Value}");
      //}
    }

    Environment.Exit(0);
  }
}