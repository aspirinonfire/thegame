using System.ComponentModel.DataAnnotations;

namespace TheGame.Infra.AppConfiguration.Sections;

public sealed record PulumiConfiguration
{
  [Required]
  public string ProjectName { get; set; } = "TheGame";
  [Required]
  public string StackName { get; set; } = default!;
  [Required]
  public string AzureNativeVersion { get; set; } = "3.5.1";
  [Required]
  public string AzureAdVersion { get; set; } = "6.5.1";
  [Required]
  [RegularExpression("^azblob://\\w+\\?storage_account=\\w+$", ErrorMessage = "Backend URL must match Azure Blob Storage Format: azblob://<container_name>?storage_account=<azure_storage_acc_name>", MatchTimeoutInMilliseconds = 1000)]
  public string BackendBlobStorageUrl { get; set; } = default!;
}
