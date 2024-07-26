using Pulumi.AzureNative.Resources.V20240301;
using Pulumi.AzureNative.Web.V20230101;
using Pulumi.AzureNative.Web.V20230101.Inputs;
using System;
using System.Collections.Generic;

return await Pulumi.Deployment.RunAsync(async () =>
{
  var config = new Pulumi.Config();
  var subscriptionId = config.Require("subscriptionId");
  var resGroupName = config.Require("resourceGroupName");

  // Get reference to an existing resource group
  var getResGroupArgs = new GetResourceGroupArgs
  {
    ResourceGroupName = resGroupName
  };

  var resGroup = await GetResourceGroup.InvokeAsync(getResGroupArgs)
    ?? throw new InvalidOperationException($"Resource group {resGroupName} was not found!");

  // TODO create ACA resources here...

  // Export outputs here
  return new Dictionary<string, object?>
  {
    ["ResourceGroupId"] = resGroup?.Id
  };
});
