using Pulumi;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Sql;
using System;

namespace TheGame.Infra;

// devnote: this file cannot inherit from Stack or it will cause duplicate urn error
// see: https://archive.pulumi.com/t/14250948/hello-any-reason-why-i-would-be-recieving-this-error-on-pulu#511b0f4a-cd53-45ca-b894-3d059fd346a4
public class TheGameStack
{
  public TheGameStack(TheGameConfig gameConfig)
  {
    // Get reference to an existing resource group
    var getResGroupArgs = new GetResourceGroupInvokeArgs
    {
      ResourceGroupName = gameConfig.ResourceGroupName
    };

    var resGroup = GetResourceGroup.Invoke(getResGroupArgs)
      ?? throw new InvalidOperationException($"Resource group {gameConfig.ResourceGroupName} was not found!");

    var resgroupName = resGroup.Apply(grp => grp.Name);

    var gameDbServer = GetServer.Invoke(new GetServerInvokeArgs()
    {
      ResourceGroupName = resgroupName,
      ServerName = gameConfig.DbServerName,
    });

    var gameDb = GetDatabase.Invoke(new GetDatabaseInvokeArgs()
    {
      ResourceGroupName = resgroupName,
      ServerName = gameDbServer.Apply(server => server.Name),
      DatabaseName = gameConfig.DbName
    });

    var containerAppEnv = new ManagedEnvironment(gameConfig.AcaEnvName, new ManagedEnvironmentArgs
    {
      ResourceGroupName = resgroupName,
      EnvironmentName = gameConfig.AcaEnvName,
      Location = gameConfig.Location,

      Sku = new EnvironmentSkuPropertiesArgs()
      {
        Name = SkuName.Consumption
      }
    });

    var gameDbServerName = gameDbServer.Apply(server => server.Name);
    var gameDbName = gameDb.Apply(db => db.Name);

    var containerApp = new ContainerApp(gameConfig.AcaName, new ContainerAppArgs()
    {
      ResourceGroupName = resgroupName,
      ContainerAppName = gameConfig.AcaName,
      Location = gameConfig.Location,
      ManagedEnvironmentId = containerAppEnv.Id,

      Identity = new ManagedServiceIdentityArgs()
      {
        Type = Pulumi.AzureNative.App.ManagedServiceIdentityType.SystemAssigned,
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

    // TODO run SQL command to assign ACA identity SQL roles
    

    ResourceGroupId = resGroup.Apply(grp => grp.Id);
    LatestRevisionFqnd = containerApp.LatestRevisionFqdn;
  }

  [Output]
  public Output<string> LatestRevisionFqnd { get; private set; }

  [Output]
  public Output<string> ResourceGroupId { get; private set; }
}
