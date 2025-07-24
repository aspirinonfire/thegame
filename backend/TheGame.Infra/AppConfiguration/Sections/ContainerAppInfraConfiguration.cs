using System.ComponentModel.DataAnnotations;

namespace TheGame.Infra.AppConfiguration.Sections;

public sealed record ContainerAppInfraConfiguration
{
  public const string GhcrPatSecretName = "ghcr-pat";

  [Required]
  public string AcaEnvName { get; set; } = default!;
  
  [Required]
  public string AcaName { get; set; } = default!;
  
  [Required]
  public string GhcrUrl { get; set; } = default!;
  
  [Required]
  public string GhcrUsername { get; set; } = default!;
  
  [Required]
  public string GhcrPat { get; set; } = default!;
  
  [Required]
  public string GameImage { get; set; } = default!;
}
