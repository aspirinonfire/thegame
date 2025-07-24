using System;
using System.ComponentModel.DataAnnotations;

namespace TheGame.Infra.AppConfiguration.Sections;

public sealed record GameApiConfiguration
{
  public const string GoogleClientSecretName = "google-client-secret";
  public const string JwtSecretName = "jwt-secret";

  [Required]
  public string GoogleClientId { get; set; } = default!;

  [Required]
  public string GoogleClientSecret { get; set; } = default!;

  [Required]
  [MinLength(32, ErrorMessage = "JWT secret must be at least 32 chars long")]
  public string JwtSecret { get; set; } = default!;

  [Required]
  public string JwtAudience { get; set; } = default!;

  [Range(1, 60 * 24, ErrorMessage = "JWT expiration must be between 1 min and 1 day")]
  public int JwtTokenExpirationMin { get; set; }
}
