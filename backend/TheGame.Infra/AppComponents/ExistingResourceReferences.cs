using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Sql;
using System;
using System.Threading.Tasks;
using TheGame.Infra.AppConfiguration;

namespace TheGame.Infra.AppComponents;

public sealed record ExistingResourceReferences(GetResourceGroupResult TargetResourceGroup, GetServerResult GameDbServer, GetDatabaseResult GameDb)
{
  public static async Task<ExistingResourceReferences> GetExistingAzureResources(TheGameInfraConfig infraConfig)
  {
    Console.WriteLine("Retrieving existing Azure resources...");

    var targetResourceGroup = await GetResourceGroup
      .InvokeAsync(new GetResourceGroupArgs()
      {
        ResourceGroupName = infraConfig.ExistingResources.ResourceGroupName
      })
      ?? throw new InvalidOperationException($"Resource group {infraConfig.ExistingResources.ResourceGroupName} was not found!");

    // TODO moved to IaC managed db server!
    
    var gameDbServer = await GetServer.InvokeAsync(new GetServerArgs()
    {
      ResourceGroupName = targetResourceGroup.Name,
      ServerName = infraConfig.AzureSqlServer.DbServerName,
    });

    var gameDb = await GetDatabase.InvokeAsync(new GetDatabaseArgs()
    {
      ResourceGroupName = targetResourceGroup.Name,
      ServerName = gameDbServer.Name,
      DatabaseName = infraConfig.AzureSqlServer.DbName
    });

    return new ExistingResourceReferences(targetResourceGroup, gameDbServer, gameDb);
  }
}
