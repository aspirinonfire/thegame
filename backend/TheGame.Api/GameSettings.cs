using System.ComponentModel.DataAnnotations;

namespace TheGame.Api;

public sealed record GameSettings
{
  [Required]
  public required AuthSettings Auth { get; init; }

  public OtelConfig? Otel { get; init; }
}

public sealed record AuthSettings
{
  [Required]
  public required ApiAuthSettings Api { get; init; }

  [Required]
  public required ApiGoogleSettings Google { get; init; }
}

public sealed record ApiAuthSettings
{
  [Required]
  public required string JwtSecret { get; init; }

  [Required]
  public required string JwtAudience { get; init; }

  [Required]
  public required ushort JwtTokenExpirationMin { get; init; }

  [Required]
  public required ushort RefreshTokenByteCount { get; init; }

  [Required]
  public required uint RefreshTokenAgeMinutes { get; init; }
}

public sealed record ApiGoogleSettings
{
  [Required]
  public required string ClientId { get; init; }

  [Required]
  public required string ClientSecret { get; init; }
}

public sealed record OtelConfig
{
  public string? ExporterEndpoint { get; init; }
  public string? ExporterApiKey { get; init; }
}
