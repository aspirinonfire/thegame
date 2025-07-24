using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using System;
using TheGame.Infra.AppConfiguration;

namespace TheGame.Infra.AppComponents;

public sealed record StaticWebAppResources(StaticSite StaticWebApp)
{
  public static StaticWebAppResources CreateStaticWebApp(ExistingResourceReferences existingReferences,
    TheGameInfraConfig infraConfig)
  {
    Console.WriteLine("Setting up Static Web App...");

    var swa = new StaticSite("app-frontend",
        new StaticSiteArgs()
        {
          Name = infraConfig.StaticWebApp.AppName,

          ResourceGroupName = existingReferences.TargetResourceGroup.Name,
          Location = existingReferences.TargetResourceGroup.Location,
          Tags = infraConfig.GetDefaultTags(),

          Sku = new SkuDescriptionArgs()
          {
            Name = infraConfig.StaticWebApp.Sku,
            Tier = infraConfig.StaticWebApp.Sku
          },

          // We will use Azure DevOps to build frontend artifacts so we won't use SWA build features.
          // However, RepositoryUrl is required to build a valid AzApi request body, even if SWA won't be linked to any repos.
          // As an official workaround, set it to an empty string.
          // See https://github.com/Azure/azure-powershell/issues/16594#issuecomment-1427828801
          RepositoryUrl = ""
        });

    // Note we are currently using free SWA + CORS in API,
    // but for $10/month we can automatically proxy all API calls without a fuss
    
    //var backendLink = new StaticSiteLinkedBackend("app-backend-link",
    //    new StaticSiteLinkedBackendArgs()
    //    {
    //      Name = swa.Name,
    //      BackendResourceId = backendApp.ApiApp.Id,
    //      LinkedBackendName = "api",

    //      ResourceGroupName = existingAzureResources.TargetResourceGroup.Name,
    //      Region = backendApp.ApiApp.Location,
    //    });

    return new StaticWebAppResources(swa);
  }
}
