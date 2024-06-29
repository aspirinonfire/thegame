using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using OneOf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace TheGame.Api.Auth
{
  public class GameAuthService
  {
    public const string PlayerIdClaimType = "game_player_id";

    public const string UnsuccessfulExternalAuthError = "External auth result was not successful!";
    public const string MissingAuthClaimsError = "Failed to retrieve claims from external auth result!";
    public const string MissingAuthNameIdClaimError = "Failed to retrieve NameId claim from external auth result!";

    // TODO read from config
    public const int MinutesBetweenRefresh = 10;

    private readonly ILogger<GameAuthService> _logger;

    public GameAuthService(ILogger<GameAuthService> logger)
    {
      _logger = logger;
    }

    public virtual async Task<OneOf<ClaimsPrincipal, string>> CreateClaimsIdentity(AuthenticateResult externalAuthResult)
    {
      if (!externalAuthResult.Succeeded)
      {
        _logger.LogError(UnsuccessfulExternalAuthError);
        return UnsuccessfulExternalAuthError;
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
        return MissingAuthClaimsError;
      }

      // uniquely identifies current user
      var authId = string.Empty;
      if (claimsLookup.TryGetValue(ClaimTypes.NameIdentifier, out var nameIdentifierClaim))
      {
        authId = $"{externalAuthResult.Principal!.Identity!.AuthenticationType}_{nameIdentifierClaim.Value}";
      }
      else
      {
        _logger.LogError(MissingAuthNameIdClaimError);
        return MissingAuthNameIdClaimError;
      }

      // TODO handle auth persistance

      // CREATE
      // 1. Player
      // 2. Team
      // 3. Auth Record + Refresh Token

      // UPDATE
      // 1. Auth Record Refresh token

      // TODO Populate Player Id claim
      var playerId = Guid.Empty.ToString();

      var claims = new List<Claim>
      {
        new(PlayerIdClaimType, playerId, "string"),
        new(ClaimTypes.NameIdentifier, authId, "string")
      };

      if (claimsLookup.TryGetValue(ClaimTypes.Name, out var playerNameClaim) && playerNameClaim != null)
      {
        claims.Add(playerNameClaim);
      }

      // TODO move to persistence rather than identity claim!
      var refreshToken = externalAuthResult.Properties.GetTokenValue(GoogleAuthConstants.RefreshTokenName);
      if (!string.IsNullOrEmpty(refreshToken))
      {
        claims.Add(new Claim(GoogleAuthConstants.RefreshTokenName, refreshToken, "string", GoogleAuthConstants.ClaimsIssuer));
      }

      var accessToken = externalAuthResult.Properties.GetTokenValue(GoogleAuthConstants.AccessTokenName);
      if (!string.IsNullOrEmpty(accessToken))
      {
        claims.Add(new Claim(GoogleAuthConstants.AccessTokenName, accessToken, "string", GoogleAuthConstants.ClaimsIssuer));
      }

      var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

      return new ClaimsPrincipal(claimsIdentity);
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
}
