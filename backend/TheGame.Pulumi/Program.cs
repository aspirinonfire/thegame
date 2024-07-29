using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.Resources.V20240301;
using System;
using System.Collections.Generic;
using Pulumi.AzureNative.App.Inputs;

return await Pulumi.Deployment.RunAsync(async () =>
{
  var config = new Pulumi.Config();

  var gameConfig = config.RequireObject<TheGameConfig>("game_config");


  // Get reference to an existing resource group
  var getResGroupArgs = new GetResourceGroupArgs
  {
    ResourceGroupName = gameConfig.resourceGroupName
  };

  var resGroup = await GetResourceGroup.InvokeAsync(getResGroupArgs)
    ?? throw new InvalidOperationException($"Resource group {gameConfig.resourceGroupName} was not found!");

  // TODO Create Azure SQL

  var containerAppEnv = new ManagedEnvironment(gameConfig.acaEnvName, new ManagedEnvironmentArgs
  {
    ResourceGroupName = resGroup.Name,
    EnvironmentName = gameConfig.acaEnvName,

    Sku = new EnvironmentSkuPropertiesArgs
    {
      Name = "Consumption"
    }
  });

  var containerApp = new ContainerApp(gameConfig.acaName, new ContainerAppArgs()
  {
    ResourceGroupName = resGroup.Name,
    ContainerAppName = gameConfig.acaName,
    ManagedEnvironmentId = containerAppEnv.Id,
    Identity = new ManagedServiceIdentityArgs()
    {
      Type = ManagedServiceIdentityType.SystemAssigned
    },

    Configuration = new ConfigurationArgs()
    {
      Ingress = new IngressArgs()
      {
        AllowInsecure = false,
        External = true,
        TargetPort = 8080,
        Transport = IngressTransportMethod.Http,
        Traffic = new TrafficWeightArgs()
        {
          Weight = 100,
          Label = "default",
          LatestRevision = true
        }
      },
      Registries = new[]
      {
        new RegistryCredentialsArgs()
        {
          Server = gameConfig.ghcrUrl,
          Username = gameConfig.ghcrUsername,
          PasswordSecretRef = "registry-password",
        }
      },
      ActiveRevisionsMode = ActiveRevisionsMode.Single,
      MaxInactiveRevisions = 1,
      Secrets = new []
      {
        new SecretArgs()
        {
          Name = "registry-password",
          Value = gameConfig.ghcrPat
        },
      }
    },

    Template = new TemplateArgs()
    {
      Scale = new ScaleArgs()
      {
        MinReplicas = 0,
        MaxReplicas = 1
      },
      

      Containers = new []
      {
        new ContainerArgs()
        {
          Name = "gameapp",
          Image = $"{gameConfig.ghcrUrl}/{gameConfig.ghcrUsername}/{gameConfig.gameImage}",
          Resources = new ContainerResourcesArgs()
          {
            Cpu = 0.25,
            Memory = "0.5Gi"
          }
        }
      }
    }
  });

  // TODO add RBAC to enabe ACA access to SQL


  // Export outputs here
  return new Dictionary<string, object?>
  {
    ["ResourceGroupId"] = resGroup?.Id,
    ["GameUrl"] = containerApp.LatestRevisionFqdn
  };
});

public sealed record TheGameConfig
{
  public string subscriptionId { get; set; } = default!;
  public string resourceGroupName { get; set; } = default!;

  public string dbName { get; set; } = default!;
  public string dbSku { get; set; } = default!;

  public string acaEnvName { get; set; } = default!;
  public string acaName { get; set; } = default!;
  public string ghcrUrl { get; set; } = default!;
  public string ghcrUsername { get; set; } = default!;
  public string ghcrPat { get; set; } = default!;
  public string gameImage { get; set; } = default!;
}