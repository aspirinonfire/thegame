using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;

namespace TheGame.Pulumi.AppComponents;

public static class GameContainerApp
{
  public static ContainerApp CreateGameContainerApp(TheGameConfig gameConfig,
    global::Pulumi.AzureNative.Resources.GetResourceGroupResult resGroup,
    ManagedEnvironment containerAppEnv,
    string gameDbServerName,
    string gameDbName)
  {
    var containerApp = new ContainerApp(gameConfig.AcaName, new ContainerAppArgs()
    {
      ResourceGroupName = resGroup.Name,
      ContainerAppName = gameConfig.AcaName,
      Location = gameConfig.Location,
      ManagedEnvironmentId = containerAppEnv.Id,
      
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
            Server = gameConfig.GhcrUrl,
            Username = gameConfig.GhcrUsername,
            PasswordSecretRef = TheGameConfig.GhcrPatSecretName,
          }
        },
        ActiveRevisionsMode = ActiveRevisionsMode.Single,
        MaxInactiveRevisions = 1,
        Secrets = new[]
        {
          new SecretArgs()
          {
            Name = TheGameConfig.GhcrPatSecretName,
            Value = gameConfig.GhcrPat
          },
          new SecretArgs()
          {
            Name = TheGameConfig.GoogleClientSecretName,
            Value = gameConfig.GoogleClientSecret,
          },
          new SecretArgs()
          {
            Name = TheGameConfig.JwtSecretName,
            Value = gameConfig.JwtSecret,
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
            Image = $"{gameConfig.GhcrUrl}/{gameConfig.GhcrUsername}/{gameConfig.GameImage}",
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
                // This is a passwordless connection string so it is safe to commit to repo since it does not contain any secrets.
                Value = $"Server=tcp:{gameDbServerName}.database.windows.net,1433;Initial Catalog={gameDbName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Managed Identity\";"
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Google__ClientId",
                Value = gameConfig.GoogleClientId
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Google__ClientSecret",
                SecretRef = TheGameConfig.GoogleClientSecretName
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Api__JwtSecret",
                SecretRef = TheGameConfig.JwtSecretName
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Api__JwtAudience",
                Value = gameConfig.JwtAudience
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Api__JwtTokenExpirationMin",
                Value = $"{gameConfig.JwtTokenExpirationMin}"
              }
            }
          }
        }
      }
    });

    return containerApp;
  }
}
