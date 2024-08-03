using Microsoft.Data.SqlClient; // IMPORTANT! not System.Data.SqlClient
using Pulumi.AzureAD;
using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Sql;
using System;
using System.Threading.Tasks;

namespace TheGame.Infra;

// devnote: this file cannot inherit from Stack or it will cause duplicate urn error
// see: https://archive.pulumi.com/t/14250948/hello-any-reason-why-i-would-be-recieving-this-error-on-pulu#511b0f4a-cd53-45ca-b894-3d059fd346a4
public sealed class TheGameStack
{
  private readonly TheGameConfig _gameConfig;

  public TheGameStack(TheGameConfig gameConfig)
  {
    _gameConfig = gameConfig;
  }

  public async Task SetupProgram()
  {
    var existingResources = await GetExistingResources();

    var containerAppEnv = new ManagedEnvironment(_gameConfig.AcaEnvName, new ManagedEnvironmentArgs
    {
      ResourceGroupName = existingResources.ResourceGroupName,
      EnvironmentName = _gameConfig.AcaEnvName,
      Location = existingResources.ResourceGroupLocation,

      Sku = new EnvironmentSkuPropertiesArgs()
      {
        Name = SkuName.Consumption
      }
    });

    var gameAppContainer = ConfigureGameAppContainerApp(existingResources, containerAppEnv);

    CreateSqlUserForContainerAppAndAssignRoles(gameAppContainer, existingResources);
  }

  private async Task<ExistingAzureResources> GetExistingResources()
  {
    // Get reference to an existing resource group
    var getResGroupArgs = new GetResourceGroupArgs
    {
      ResourceGroupName = _gameConfig.ResourceGroupName
    };

    var resGroup = await GetResourceGroup.InvokeAsync(getResGroupArgs)
      ?? throw new InvalidOperationException($"Resource group {_gameConfig.ResourceGroupName} was not found!");

    var gameDbServer = await GetServer.InvokeAsync(new GetServerArgs()
    {
      ResourceGroupName = resGroup.Name,
      ServerName = _gameConfig.DbServerName,
    });

    var gameDb = await GetDatabase.InvokeAsync(new GetDatabaseArgs()
    {
      ResourceGroupName = resGroup.Name,
      ServerName = gameDbServer.Name,
      DatabaseName = _gameConfig.DbName
    });

    return new ExistingAzureResources(resGroup.Name, resGroup.Id, resGroup.Location, gameDbServer.Name, gameDb.Name);
  }

  private ContainerApp ConfigureGameAppContainerApp(ExistingAzureResources existingResources, ManagedEnvironment containerAppEnv)
  {
    var containerApp = new ContainerApp(_gameConfig.AcaName, new ContainerAppArgs()
    {
      ResourceGroupName = existingResources.ResourceGroupName,
      ContainerAppName = _gameConfig.AcaName,
      Location = existingResources.ResourceGroupLocation,
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
            Server = _gameConfig.GhcrUrl,
            Username = _gameConfig.GhcrUsername,
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
            Value = _gameConfig.GhcrPat
          },
          new SecretArgs()
          {
            Name = TheGameConfig.GoogleClientSecretName,
            Value = _gameConfig.GoogleClientSecret,
          },
          new SecretArgs()
          {
            Name = TheGameConfig.JwtSecretName,
            Value = _gameConfig.JwtSecret,
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
            Image = $"{_gameConfig.GhcrUrl}/{_gameConfig.GhcrUsername}/{_gameConfig.GameImage}",
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
                Value = GetSqlConnectionString(existingResources, "Active Directory Managed Identity")
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Google__ClientId",
                Value = _gameConfig.GoogleClientId
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
                Value = _gameConfig.JwtAudience
              },
              new EnvironmentVarArgs()
              {
                Name = "Auth__Api__JwtTokenExpirationMin",
                Value = $"{_gameConfig.JwtTokenExpirationMin}"
              }
            }
          }
        }
      }
    });

    return containerApp;
  }

  private void CreateSqlUserForContainerAppAndAssignRoles(ContainerApp containerApp, ExistingAzureResources existingResources)
  {
    // Managed identity name (not id) _usually_ matches ACA name. Querying identity name _from_ principal id requires elevated privilidges
    // Not gonna touch this unless it becomes a problem in the future.
    var entraAppObjectId = containerApp.Name.Apply(async acaIdentityName =>
    {
      // Create user script will authenticate with SQL server using it identity established with 'az login'
      // we'll use 2min timeout to allow serverless db to back to life. container app has db retries enabled so it can tolerate shorter timeout
      var connectionString = GetSqlConnectionString(existingResources, "Active Directory Default", 120);

      await AssignSqlRwToExternalIdentity(connectionString, acaIdentityName);

    });
  }

  public static async Task AssignSqlRwToExternalIdentity(string connectionString, string externalUserName)
  {
    using var connection = new SqlConnection(connectionString);
    
    // TODO figure out better resilience solution
    for (var attempt = 0; attempt < 10; attempt++)
    {
      try
      {
        await connection.OpenAsync();
        break;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to connect to SQL Server: {ex.GetType().Name}: {ex.Message}");
      }
      await Task.Delay(TimeSpan.FromSeconds(10));
    }

    using var transaction = connection.BeginTransaction();
    try
    {
      Console.WriteLine($"Attempting to add ACA identity {externalUserName} to SQL user roles...");
      using var command = connection.CreateCommand();
      command.Transaction = transaction;
      // SQL statement needs to be idempotent!
      command.CommandText = @"
            DECLARE @sqlCreateUser nvarchar(max)
            DECLARE @sqlDataReader nvarchar(max)
            DECLARE @sqlDataWriter nvarchar(max)

            SET @sqlCreateUser = 'CREATE USER ' + QUOTENAME(@UserName) + ' FROM EXTERNAL PROVIDER';
            SET @sqlDataReader = 'ALTER ROLE db_datareader ADD MEMBER ' + QUOTENAME(@UserName);
            SET @sqlDataWriter = 'ALTER ROLE db_datawriter ADD MEMBER ' + QUOTENAME(@UserName);

            IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @UserName)
            BEGIN
              EXEC(@sqlCreateUser);
              EXEC(@sqlDataReader);
              EXEC(@sqlDataWriter);
            END";
      command.Parameters.AddWithValue("@UserName", externalUserName);

      await command.ExecuteNonQueryAsync();
      await transaction.CommitAsync();
      Console.WriteLine("ACA identity has been added to SQL user roles...");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to add ACA identity to SQL user roles: {ex.GetType().Name}-{ex.Message}");
      await transaction.RollbackAsync();
      throw;
    }
  }

  // This is a passwordless connection string so it is safe to commit to repo since it does not contain any secrets.
  public static string GetSqlConnectionString(ExistingAzureResources existingResources, string sqlAuthType, int connectionTimeoutSec = 30) =>
    $"Server={existingResources.AzureSqlServerName}.database.windows.net; Authentication={sqlAuthType}; Database={existingResources.AzureSqlDbName}; Encrypt=True; TrustServerCertificate=False; Connection Timeout={connectionTimeoutSec};";
}
