using Pulumi.AzureNative.App.V20240202Preview;
using Pulumi.AzureNative.App.V20240202Preview.Inputs;

namespace TheGame.Pulumi.AppComponents;

public static class GameContainerApp
{
  public static ContainerApp CreateGameContainerApp(TheGameConfig gameConfig,
    global::Pulumi.AzureNative.Resources.GetResourceGroupResult resGroup,
    global::Pulumi.AzureNative.App.ManagedEnvironment containerAppEnv,
    string gameDbServerName,
    string gameDbName)
  {
    // this connection string will not contain username and password and is therefore can be stored safely in repo
    var connectionString = $"Server=tcp:{gameDbServerName}.database.windows.net,1433;Initial Catalog={gameDbName};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"Active Directory Managed Identity\";";


    var appImage = $"{gameConfig.GhcrUrl}/{gameConfig.GhcrUsername}/{gameConfig.GameImage}";
    var connStringAppEnvVar = new EnvironmentVarArgs()
    {
      Name = "ConnectionStrings__GameDB",
      Value = connectionString
    };

    var containerApp = new ContainerApp(gameConfig.AcaName, new ContainerAppArgs()
    {
      ResourceGroupName = resGroup.Name,
      ContainerAppName = gameConfig.AcaName,
      ManagedEnvironmentId = containerAppEnv.Id,
      Identity = new ManagedServiceIdentityArgs()
      {
        Type = ManagedServiceIdentityType.SystemAssigned,
      },

      Configuration = new ConfigurationArgs()
      {
        // ensure managed identity is set on both main AND init containers
        // this is critical for db migrations
        IdentitySettings = new[]
        {
          new IdentitySettingsArgs()
          {
            Identity = "system",
            Lifecycle = IdentitySettingsLifeCycle.All
          }
        },
        
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
            Image = appImage,
            Resources = new ContainerResourcesArgs()
            {
              Cpu = 0.25,
              Memory = "0.5Gi"
            },
            Env = new []
            {
              connStringAppEnvVar,
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
        },
        
        InitContainers = new[]
        {
          new InitContainerArgs()
          {
            Name = "gameapp-db-mig",
            Image = appImage,
            Command = new []
            {
              "dotnet",
              "TheGame.Api.dll",
              "--migrate-db"
            },
            Resources = new ContainerResourcesArgs()
            {
              Cpu = 0.25,
              Memory = "0.5Gi"
            },
            Env = new [] { connStringAppEnvVar }
          }
        }
      }
    });

    return containerApp;
  }
}
