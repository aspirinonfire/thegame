using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using OneOf;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public class GameAuthService
{
  public const string PlayerIdentityIdClaimType = "game_player_identity_id";
  public const string PlayerIdClaimType = "game_player_id";

  public const string UnsuccessfulExternalAuthError = "External auth result was not successful!";
  public const string MissingAuthClaimsError = "Failed to retrieve claims from external auth result!";
  public const string MissingAuthNameIdClaimError = "Failed to retrieve NameId claim from external auth result!";
  public const string MissingIdentityNameClaimError = "Failed to retrieve Player Name from external auth result!";

  // TODO read from config
  public const int MinutesBetweenRefresh = 10;

  private readonly ILogger<GameAuthService> _logger;

  public GameAuthService(ILogger<GameAuthService> logger)
  {
    _logger = logger;
  }

  public virtual OneOf<GetOrCreateNewPlayerCommand, Failure> GenerateGetOrCreateNewPlayerCommand(AuthenticateResult externalAuthResult)
  {
    if (!externalAuthResult.Succeeded)
    {
      _logger.LogError(UnsuccessfulExternalAuthError);
      return new Failure(UnsuccessfulExternalAuthError);
    }

    var claimsLookup = externalAuthResult
      .Principal?
      .Identities?
      .FirstOrDefault()?
      .Claims
      .ToDictionary(claim => claim.Type, claim => claim);

    if (claimsLookup == null || !claimsLookup.Any())
    {
      _logger.LogError(MissingAuthClaimsError);
      return new Failure(MissingAuthClaimsError);
    }

    if (!claimsLookup.TryGetValue(ClaimTypes.NameIdentifier, out var nameIdentifierClaim) || string.IsNullOrEmpty(nameIdentifierClaim.Value))
    {
      _logger.LogError(MissingAuthNameIdClaimError);
      return new Failure(MissingAuthNameIdClaimError);
    }

    var refreshToken = externalAuthResult.Properties.GetTokenValue(GoogleAuthConstants.RefreshTokenName);
    if (string.IsNullOrEmpty(refreshToken))
    {
      _logger.LogWarning("Refresh Token is missing for {providerIdentityId}", nameIdentifierClaim.Value);
    }

    if (!claimsLookup.TryGetValue(ClaimTypes.Name, out var playerNameClaim) || string.IsNullOrEmpty(playerNameClaim.Value))
    {
      return new Failure(MissingIdentityNameClaimError);
    }

    var request = new NewPlayerIdentityRequest(externalAuthResult.Principal!.Identity!.AuthenticationType ?? "unknown",
      nameIdentifierClaim.Value,
      refreshToken ?? string.Empty,
      playerNameClaim.Value);

    return new GetOrCreateNewPlayerCommand(request);
  }

  public virtual async Task RefreshCookie(CookieValidatePrincipalContext ctx)
  {
    // check if principal needs to be re-authed every x minutes
    var minutesSinceLastRefresh = (DateTimeOffset.UtcNow - ctx.Properties.IssuedUtc.GetValueOrDefault()).TotalMinutes;
    if (minutesSinceLastRefresh < MinutesBetweenRefresh)
    {
      // current principal is considered to be valid.
      return;
    }

    _logger.LogInformation("Validating user session...");

    // TODO refresh tokens and renew principal
  }
}
