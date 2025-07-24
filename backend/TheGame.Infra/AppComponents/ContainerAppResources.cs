using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using TheGame.Infra.AppConfiguration;
using TheGame.Infra.AppConfiguration.Sections;

namespace TheGame.Infra.AppComponents;

public sealed record ContainerAppResources(ContainerApp ContainerApp)
{
  public static ContainerAppResources CreateContainerApp(ExistingResourceReferences existingResources,
    TheGameInfraConfig infraConfig,
    AppEnvironmentResources appEnvironment,
    StaticWebAppResources swa)
  {
    var containerApp = new ContainerApp(infraConfig.ContainerApp.AcaName, new ContainerAppArgs()
    {
      ResourceGroupName = existingResources.TargetResourceGroup.Name,
      ContainerAppName = infraConfig.ContainerApp.AcaName,
      Location = existingResources.TargetResourceGroup.Location,
      ManagedEnvironmentId = appEnvironment.ContainerAppEnv.Id,

      Identity = new ManagedServiceIdentityArgs()
      {
        Type = ManagedServiceIdentityType.SystemAssigned,
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
            Server = infraConfig.ContainerApp.GhcrUrl,
            Username = infraConfig.ContainerApp.GhcrUsername,
            PasswordSecretRef = ContainerAppInfraConfiguration.GhcrPatSecretName,
          }
        },
        ActiveRevisionsMode = ActiveRevisionsMode.Single,
        MaxInactiveRevisions = 1,
        Secrets = new[]
        {
          new SecretArgs()
          {
            Name = ContainerAppInfraConfiguration.GhcrPatSecretName,
            Value = infraConfig.ContainerApp.GhcrPat
          },
          new SecretArgs()
          {
            Name = GameApiConfiguration.GoogleClientSecretName,
            Value = infraConfig.GameApi.GoogleClientSecret,
          },
          new SecretArgs()
          {
            Name = GameApiConfiguration.JwtSecretName,
            Value = infraConfig.GameApi.JwtSecret,
          }
        }
      },

      Template = new TemplateArgs()
      {
        Scale = new ScaleArgs()
        {
          MinReplicas = 0,
          MaxReplicas = 1
        },

        Containers = new[]
        {
          new ContainerArgs()
          {
            Name = "gameapp",
            Image = $"{infraConfig.ContainerApp.GhcrUrl}/{infraConfig.ContainerApp.GhcrUsername}/{infraConfig.ContainerApp.GameImage}",
            Resources = new ContainerResourcesArgs()
            {
              Cpu = 0.25,
              Memory = "0.5Gi"
            },

            Env = new []
            {
              new EnvironmentVarArgs()
              {
                Name = "ConnectionStrings__GameDB",
                // ACA will authenticate with SQL server using its System Assigned Managed Identity
                Value = SqlServerResources.GetSqlConnectionString(existingResources, "Active Directory Managed Identity")
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Google__ClientId",
                Value = infraConfig.GameApi.GoogleClientId
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Google__ClientSecret",
                SecretRef = GameApiConfiguration.GoogleClientSecretName
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Api__JwtSecret",
                SecretRef = GameApiConfiguration.JwtSecretName
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Api__JwtAudience",
                Value = infraConfig.GameApi.JwtAudience
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Api__JwtTokenExpirationMin",
                Value = $"{infraConfig.GameApi.JwtTokenExpirationMin}"
              },
              new EnvironmentVarArgs()
              {
                Name = "Cors__uiHost",
                Value = swa.StaticWebApp.DefaultHostname
              },
            }
          }
        }
      }
    });

    return new ContainerAppResources(containerApp);
  }
}
