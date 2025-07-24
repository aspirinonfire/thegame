using System.ComponentModel.DataAnnotations;

namespace TheGame.Infra.AppConfiguration.Sections;

public sealed record StaticWebAppConfig
{
  [Required]
  public string AppName { get; set; } = default!;

  [Required]
  public string Sku { get; set; } = "Free";
}
