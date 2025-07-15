using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public sealed record ApiTokens(bool IsNewIdentity, string AccessToken);

public sealed record ValidatedAccessTokenValues(long PlayerId, string PlayerIdentityName, string PlayerIdentityId);

public interface IGameAuthService
{
  string GenerateApiJwtToken(string providerName, string providerIdentityId, long playerId, long playerIdentityId);
  Result<ValidatedAccessTokenValues> GetValidateExpiredAccessToken(string accessToken);
  string RetrieveRefreshTokenValue(HttpContext httpContext);
  void SetRefreshCookie(HttpContext httpContext, string refreshTokenValue, TimeSpan tokenExpiration);
}

public class GameAuthService(ILogger<GameAuthService> logger,
  IOptions<GameSettings> gameSettings) : IGameAuthService
{
  // TODO figure out correct way to set issuer (config vs api host, etc).
  public const string ValidApiTokenIssuer = "this-is-valid-issuer";

  public static SymmetricSecurityKey GetAccessTokenSigningKey(string jwtSecret) =>
    new(Encoding.UTF8.GetBytes(jwtSecret));

  public static TokenValidationParameters GetTokenValidationParams(string jwtAudience, string jwtSecret, string jwtIssuer) =>
    new()
    {
      ValidateIssuer = true,
      ValidIssuer = jwtIssuer,
      ValidateLifetime = true,
      ValidateAudience = true,
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = GetAccessTokenSigningKey(jwtSecret),
      ValidAudience = jwtAudience
    };

  public const string ApiRefreshTokenCookieName = "gameapi-refresh";

  public const string PlayerIdentityAuthority = "iden_authority";
  public const string PlayerIdentityUserId = "iden_user_id";
  public const string PlayerIdentityIdClaimType = "player_identiy_id";
  public const string PlayerIdClaimType = "player_id";

  public const string InvalidRefreshParameters = "access_refresh_parameters_invalid";

  public Result<ValidatedAccessTokenValues> GetValidateExpiredAccessToken(string accessToken)
  {
    long playerId = 0;
    string? identityProvider;
    string? identityId;
    try
    {
      // validate token
      var jwtValidationParams = GetTokenValidationParams(gameSettings.Value.Auth.Api.JwtAudience,
        gameSettings.Value.Auth.Api.JwtSecret,
        ValidApiTokenIssuer);

      // expired tokens are ok
      jwtValidationParams.ValidateLifetime = false;

      var tokenClaims = new JwtSecurityTokenHandler()
        .ValidateToken(accessToken, jwtValidationParams, out _);

      // extract player id claim
      var playerIdClaim = tokenClaims.FindFirstValue(PlayerIdClaimType);
      if (!string.IsNullOrWhiteSpace(playerIdClaim))
      {
        long.TryParse(playerIdClaim, null, out playerId);
      }

      identityProvider = tokenClaims.FindFirstValue(PlayerIdentityAuthority);

      identityId = tokenClaims.FindFirstValue(PlayerIdentityUserId);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to validate access token.");
      return new Failure(InvalidRefreshParameters);
    }

    if (playerId < 1)
    {
      logger.LogError("PlayerId claim value is invalid.");
      return new Failure(InvalidRefreshParameters);
    }

    if (string.IsNullOrWhiteSpace(identityProvider))
    {
      logger.LogError("Player Identity provider value is invalid.");
      return new Failure(InvalidRefreshParameters);
    }

    if (string.IsNullOrWhiteSpace(identityId))
    {
      logger.LogError("Player Identity ID value is invalid.");
      return new Failure(InvalidRefreshParameters);
    }

    return new ValidatedAccessTokenValues(playerId, identityProvider, identityId);
  }

  /// <summary>
  /// Generate API token using JWT format. This token is expected to be used during Game API authentication.
  /// </summary>
  /// <param name="providerName"></param>
  /// <param name="providerIdentityId"></param>
  /// <param name="playerId"></param>
  /// <param name="playerIdentityId"></param>
  /// <returns></returns>
  public string GenerateApiJwtToken(string providerName, string providerIdentityId, long playerId, long playerIdentityId)
  {
    var claims = new List<Claim>
    {
      new(PlayerIdentityAuthority, providerName, ClaimValueTypes.String),
      new(PlayerIdentityUserId, providerIdentityId, ClaimValueTypes.String),
      new(PlayerIdClaimType, $"{playerId}", ClaimValueTypes.String),
      new(PlayerIdentityIdClaimType, $"{playerIdentityId}", ClaimValueTypes.String),
    };

    var jwtToken = new JwtSecurityToken(
      claims: claims,
      notBefore: DateTime.UtcNow,
      expires: DateTime.UtcNow.AddMinutes(gameSettings.Value.Auth.Api.JwtTokenExpirationMin),
      issuer: ValidApiTokenIssuer,
      audience: gameSettings.Value.Auth.Api.JwtAudience,
      signingCredentials: new SigningCredentials(
        GetAccessTokenSigningKey(gameSettings.Value.Auth.Api.JwtSecret),
        SecurityAlgorithms.HmacSha256Signature)
      );

    return new JwtSecurityTokenHandler().WriteToken(jwtToken);
  }

  public void SetRefreshCookie(HttpContext httpContext, string refreshTokenValue, TimeSpan tokenExpiration)
  {
    var refreshCookieOptions = new CookieBuilder()
    {
      Name = ApiRefreshTokenCookieName,
      IsEssential = true,
      HttpOnly = true,
      // refresh cookie should be sent to this api path only!
      Path = "/api/user/refresh-token",
      MaxAge = tokenExpiration,
      Expiration = tokenExpiration,
      SameSite = SameSiteMode.Strict,
      SecurePolicy = CookieSecurePolicy.Always
    }.Build(httpContext);

    httpContext.Response.Cookies.Append(ApiRefreshTokenCookieName, refreshTokenValue, refreshCookieOptions);
  }

  public string RetrieveRefreshTokenValue(HttpContext httpContext) => httpContext.Request.Cookies
      .Where(cookie => cookie.Key == ApiRefreshTokenCookieName)
      .Select(cookie => cookie.Value)
      .FirstOrDefault(string.Empty);
}
