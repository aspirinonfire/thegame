using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using TheGame.Infra.AppConfiguration;

namespace TheGame.Infra.AppComponents;

public sealed record AppEnvironmentResources(ManagedEnvironment ContainerAppEnv)
{
  public static AppEnvironmentResources CreateContainerAppEnvironment(ExistingResourceReferences existingResources,
    TheGameInfraConfig infraConfig)
  {
    var containerAppEnv = new ManagedEnvironment(infraConfig.ContainerApp.AcaEnvName,
      new ManagedEnvironmentArgs()
      {
        ResourceGroupName = existingResources.TargetResourceGroup.Name,
        Location = existingResources.TargetResourceGroup.Location,
        Tags = infraConfig.GetDefaultTags(),

        EnvironmentName = infraConfig.ContainerApp.AcaEnvName,

        WorkloadProfiles =
        {
          new WorkloadProfileArgs()
          {
              Name = "Consumption",
              WorkloadProfileType = "Consumption"
          }
        }

        // TODO add logging
      });

    return new AppEnvironmentResources(containerAppEnv);
  }
}
