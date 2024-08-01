using Pulumi.AzureNative.App;
using Pulumi.AzureNative.App.Inputs;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.Sql;
using System;
using System.Collections.Generic;
using TheGame.Pulumi;
using TheGame.Pulumi.AppComponents;

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
  
  var containerAppEnv = new ManagedEnvironment(gameConfig.AcaEnvName, new ManagedEnvironmentArgs
  {
    ResourceGroupName = resGroup.Name,
    EnvironmentName = gameConfig.AcaEnvName,
    Location = gameConfig.Location,

    Sku = new EnvironmentSkuPropertiesArgs()
    {
      Name = SkuName.Consumption
    }
  });

  var containerApp = GameContainerApp.CreateGameContainerApp(gameConfig, resGroup, containerAppEnv, gameDbServer.Name, gameDb.Name);

  // Export outputs here
  return new Dictionary<string, object?>
  {
    ["ResourceGroupId"] = resGroup.Id,
    ["RevisionGameUrl"] = containerApp.LatestRevisionFqdn
  };
});