using MediatR;
using System.ComponentModel.DataAnnotations;

namespace TheGame.Api;

public sealed class GameSettings
{
  [Required]
  public required AuthSettings Auth { get; init; }
}

public sealed class AuthSettings
{
  [Required]
  public required ApiAuthSettings Api { get; init; }

  [Required]
  public required ApiGoogleSettings Google { get; init; }
}

public sealed class ApiAuthSettings
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

public sealed class ApiGoogleSettings
{
  [Required]
  public required string ClientId { get; init; }

  [Required]
  public required string ClientSecret { get; init; }
}
