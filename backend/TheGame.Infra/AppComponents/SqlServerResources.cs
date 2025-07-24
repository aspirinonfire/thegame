using Microsoft.Data.SqlClient;
using Pulumi.AzureNative.App;
using System;
using System.Threading.Tasks;

namespace TheGame.Infra.AppComponents;

public sealed record SqlServerResources()
{
  public static void CreateSqlUserForContainerAppAndAssignRoles(ContainerApp containerApp, ExistingResourceReferences existingResources)
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

  public static string GetSqlConnectionString(ExistingResourceReferences existingResources, string sqlAuthType, int connectionTimeoutSec = 30) =>
    $"Server={existingResources.GameDbServer.Name}.database.windows.net; Authentication={sqlAuthType}; Database={existingResources.GameDb.Name}; Encrypt=True; TrustServerCertificate=False; Connection Timeout={connectionTimeoutSec};";

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
}
