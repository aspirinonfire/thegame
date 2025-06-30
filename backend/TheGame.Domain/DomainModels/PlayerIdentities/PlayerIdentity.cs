using System;
using System.Security.Cryptography;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.PlayerIdentities;

/// <summary>
/// Player identity record.
/// </summary>
/// <remarks>
/// This entity may be better managed by a separate DbContext but for the simplicity sake its all in one place.
/// </remarks>
public partial class PlayerIdentity : IAuditedRecord
{
  public const ushort MinTokenByteLenght = 64;
  public const ushort MinTokenAgeMin = 1;

  public long Id { get; protected set; }

  public Player Player { get; protected set; } = default!;

  public string ProviderName { get; protected set; } = default!;

  public string ProviderIdentityId { get; protected set; } = default!;

  public string? RefreshToken { get; protected set; } = default!;

  public DateTimeOffset? RefreshTokenExpiration { get; protected set; } = default!;

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }

  public Result<Success> RotateRefreshToken(TimeProvider timeProvider, ushort newTokenLenght, TimeSpan tokenAge)
  {
    if (newTokenLenght < MinTokenByteLenght)
    {
      return new Failure(ErrorMessageProvider.InvalidNewTokenError);
    }

    if (tokenAge.TotalMinutes < MinTokenAgeMin)
    {
      return new Failure(ErrorMessageProvider.InvalidNewTokenAgeError);
    }

    var randomBytes = RandomNumberGenerator.GetBytes(newTokenLenght);

    RefreshToken = Convert.ToBase64String(randomBytes);
    RefreshTokenExpiration = timeProvider.GetUtcNow().Add(tokenAge);

    return new Success();
  }

  public Result<Success> InvalidateRefreshToken()
  {
    RefreshToken = null;
    RefreshTokenExpiration = null;

    return new Success();
  }
}
