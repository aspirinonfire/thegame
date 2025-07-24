using System.ComponentModel.DataAnnotations;

namespace TheGame.Infra.AppConfiguration.Sections;

public sealed record ExistingResourcesConfiguration
{
  [Required]
  public string SubscriptionId { get; set; } = default!;

  [Required]
  public string ResourceGroupName { get; set; } = default!;
}
