using System.ComponentModel.DataAnnotations;

namespace TheGame.Infra.AppConfiguration.Sections;

public sealed record AzureSqlConfiguration
{
  [Required]
  public string DbServerName { get; set; } = default!;
  [Required]
  public string DbName { get; set; } = default!;
}
