using System.ComponentModel.DataAnnotations;

namespace TheGame.Api;

public sealed record GameSettings
{
  [Required]
  public required AuthSettings Auth { get; init; }

  public OtelConfig? Otel { get; init; }

  public MessageBusSettings DomainEventsMessageBus { get; init; } = new();

  public CorsSettings? Cors { get; init; }
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
  [MinLength(32, ErrorMessage = "JWT Key must be 32 chars or 256 bit long")]
  public required string JwtSecret { get; init; }

  [Required]
  public required string JwtAudience { get; init; }

  [Required]
  public required ushort JwtTokenExpirationMin { get; init; }

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

public sealed record MessageBusSettings
{
  public int MaxQueueSize { get; init; } = 10;
}

public sealed record CorsSettings
{
  public string? UiHost { get; init; }
}