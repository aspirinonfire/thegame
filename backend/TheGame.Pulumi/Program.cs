using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.Resources.V20240301;
using System;
using System.Collections.Generic;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.AzureData;
using Pulumi.AzureNative.Authorization;

return await Pulumi.Deployment.RunAsync(async () =>
{
  var config = new Pulumi.Config();

  var gameConfig = config.RequireObject<TheGameConfig>("game_config");

  // Get reference to an existing resource group
  var getResGroupArgs = new GetResourceGroupArgs
  {
    ResourceGroupName = gameConfig.ResourceGroupName
  };

  var resGroup = await GetResourceGroup.InvokeAsync(getResGroupArgs)
    ?? throw new InvalidOperationException($"Resource group {gameConfig.ResourceGroupName} was not found!");

  // TODO Create Azure SQL with Pulumi
  var gameDbServer = await GetServer.InvokeAsync(new GetServerArgs()
  {
    ResourceGroupName = resGroup.Name,
    ServerName = gameConfig.DbServerName,
  });

  var gameDb = await GetDatabase.InvokeAsync(new GetDatabaseArgs()
  {
    ResourceGroupName = resGroup.Name,
    ServerName = gameDbServer.Name,
    DatabaseName = gameConfig.DbName
  });

  // this connection string will not contain username and password and is therefore can be stored safely in repo
  var connectionString = $"Server=tcp:{gameDbServer.Name}.database.windows.net,1433;Initial Catalog={gameDb.Name};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=\"ctive Directory Managed Identity\";";

  var containerAppEnv = new ManagedEnvironment(gameConfig.AcaEnvName, new ManagedEnvironmentArgs
  {
    ResourceGroupName = resGroup.Name,
    EnvironmentName = gameConfig.AcaEnvName,

    Sku = new EnvironmentSkuPropertiesArgs
    {
      Name = "Consumption"
    }
  });

  var containerApp = new ContainerApp(gameConfig.AcaName, new ContainerAppArgs()
  {
    ResourceGroupName = resGroup.Name,
    ContainerAppName = gameConfig.AcaName,
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
          Server = gameConfig.GhcrUrl,
          Username = gameConfig.GhcrUsername,
          PasswordSecretRef = TheGameConfig.GhcrPatSecretName,
        }
      },
      ActiveRevisionsMode = ActiveRevisionsMode.Single,
      MaxInactiveRevisions = 1,
      Secrets = new []
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

      Containers = new []
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
              Value = connectionString
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
              Value = TheGameConfig.JwtSecretName
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
        // use same API image to run migrations
        new InitContainerArgs()
        {
          Name = "gameapp-db-mig",
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
              Value = connectionString
            },
          },
          Command = new []
          {
            "dotnet",
            "TheGame.Api.dll",
            "--migrate-db"
          }
        }
      }
    }
  });

  // Export outputs here
  return new Dictionary<string, object?>
  {
    ["ResourceGroupId"] = resGroup?.Id,
    ["GameUrl"] = containerApp.LatestRevisionFqdn
  };
});

public sealed record TheGameConfig
{
  public const string GhcrPatSecretName = "ghcr-pat";
  public const string GoogleClientSecretName = "google-client-secret";
  public const string JwtSecretName = "jwt-secret";

  // Azure Environment
  public string SubscriptionId { get; set; } = default!;
  public string ResourceGroupName { get; set; } = default!;

  // Azure SQL
  public string DbServerName { get; set; } = default!;
  public string DbName { get; set; } = default!;
  public string DbSku { get; set; } = default!;

  // Azure Container Apps
  public string AcaEnvName { get; set; } = default!;
  public string AcaName { get; set; } = default!;
  public string GhcrUrl { get; set; } = default!;
  public string GhcrUsername { get; set; } = default!;
  public string GhcrPat { get; set; } = default!;
  public string GameImage { get; set; } = default!;

  // Game Auth
  public string GoogleClientId { get; set; } = default!;
  public string GoogleClientSecret { get; set; } = default!;
  public string JwtSecret { get; set; } = default!;
  public string JwtAudience { get; set; } = default!;
  public int JwtTokenExpirationMin { get; set; }
}