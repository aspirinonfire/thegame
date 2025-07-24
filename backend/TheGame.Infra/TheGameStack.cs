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
  }
}
