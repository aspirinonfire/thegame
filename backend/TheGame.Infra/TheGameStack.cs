using System;
using System.IO;
using System.Threading.Tasks;
using TheGame.Infra.AppComponents;
using TheGame.Infra.AppConfiguration;

namespace TheGame.Infra;

// devnote: this file cannot inherit from Stack or it will cause duplicate urn error
// see: https://archive.pulumi.com/t/14250948/hello-any-reason-why-i-would-be-recieving-this-error-on-pulu#511b0f4a-cd53-45ca-b894-3d059fd346a4
public sealed class TheGameStack(TheGameInfraConfig infraConfig)
{
  public async Task SetupProgram()
  {
    var existingResources = await ExistingResourceReferences.GetExistingAzureResources(infraConfig);

    var swa = StaticWebAppResources.CreateStaticWebApp(existingResources, infraConfig);

    var appEnv = AppEnvironmentResources.CreateContainerAppEnvironment(existingResources, infraConfig);

    var containerApp = ContainerAppResources.CreateContainerApp(existingResources, infraConfig, appEnv, swa);

    SqlServerResources.CreateSqlUserForContainerAppAndAssignRoles(containerApp.ContainerApp, existingResources);

    containerApp.ContainerApp.Configuration.Apply(cfg =>
    {
      if (cfg?.Ingress is null)
      {
        throw new InvalidOperationException("Container App ingress is null!");
      }

      // 1) Make the key an env‑var (same‑job scope)
      var envFile = Environment.GetEnvironmentVariable("GITHUB_ENV");
      if (!string.IsNullOrWhiteSpace(envFile))
        File.AppendAllText(envFile, $"GAME_API_URL={cfg.Ingress.Fqdn}{Environment.NewLine}");

      // 2) Also expose it as a step output (so other jobs can consume it)
      var outFile = Environment.GetEnvironmentVariable("GITHUB_OUTPUT");
      if (!string.IsNullOrWhiteSpace(outFile))
        File.AppendAllText(outFile, $"game_api_url={cfg.Ingress.Fqdn}{Environment.NewLine}");

      return Task.CompletedTask;
    });
  }
}
