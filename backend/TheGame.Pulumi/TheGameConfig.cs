namespace TheGame.Pulumi;

public sealed record TheGameConfig
{
  public const string GhcrPatSecretName = "ghcr-pat";
  public const string GoogleClientSecretName = "google-client-secret";
  public const string JwtSecretName = "jwt-secret";

  // Azure Environment
  public string SubscriptionId { get; set; } = default!;
  public string ResourceGroupName { get; set; } = default!;
  public string Location { get; set; } = "westus2";

  // Azure SQL
  public string DbServerName { get; set; } = default!;
  public string DbName { get; set; } = default!;
  public string DbSku { get; set; } = default!;

  // Azure Container Apps
  public string AcaEnvName { get; set; } = default!;
  public string AcaName { get; set; } = default!;
  public string GhcrUrl { get; set; } = default!;
  public string GhcrUsername { get; set; } = default!;
  public string GhcrPat { get; set; } = default!;
  public string GameImage { get; set; } = default!;

  // Game Auth
  public string GoogleClientId { get; set; } = default!;
  public string GoogleClientSecret { get; set; } = default!;
  public string JwtSecret { get; set; } = default!;
  public string JwtAudience { get; set; } = default!;
  public int JwtTokenExpirationMin { get; set; }
}