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
using System.Threading;
using System.Threading.Tasks;
using TheGame.Api.Endpoints.User;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public sealed record ApiTokens(bool IsNewIdentity, string AccessToken);

public interface IGameAuthService
{
  string GenerateApiJwtToken(string providerName, string providerIdentityId, long playerId, long playerIdentityId);
  Task<Result<ApiTokens>> RefreshAccessToken(HttpContext httpContext, string accessToken);
  void SetRefreshCookie(HttpContext httpContext, string refreshTokenValue, TimeSpan tokenExpiration);
}

public class GameAuthService(ILogger<GameAuthService> logger,
  ICommandHandler<RotatePlayerIdentityRefreshTokenCommand, RotatePlayerIdentityRefreshTokenCommand.Result> rotateRefreshTokenHandler,
  TimeProvider timeProvider,
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

  public virtual async Task<Result<ApiTokens>> RefreshAccessToken(HttpContext httpContext, string accessToken)
  {
    var refreshCookieValue = httpContext.Request.Cookies
      .Where(cookie => cookie.Key == ApiRefreshTokenCookieName)
      .Select(cookie => cookie.Value)
      .FirstOrDefault();

    if (string.IsNullOrEmpty(refreshCookieValue) || string.IsNullOrWhiteSpace(accessToken))
    {
      return new Failure(InvalidRefreshParameters);
    }

    long playerId = 0;
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

    var refreshTokenResult = await rotateRefreshTokenHandler.Execute(
      new RotatePlayerIdentityRefreshTokenCommand(playerId,
        refreshCookieValue,
        gameSettings.Value.Auth.Api.RefreshTokenByteCount,
        gameSettings.Value.Auth.Api.RefreshTokenAgeMinutes),
      CancellationToken.None);
    if (!refreshTokenResult.TryGetSuccessful(out var newRefreshToken, out var refreshFailure))
    {
      return refreshFailure;
    }

    var cookieExpiration = newRefreshToken.RefreshTokenExpiration - timeProvider.GetUtcNow();

    SetRefreshCookie(httpContext, newRefreshToken.RefreshToken, cookieExpiration);

    var apiToken = GenerateApiJwtToken(newRefreshToken.ProviderName,
      newRefreshToken.ProviderIdentityId,
      newRefreshToken.PlayerId,
      newRefreshToken.PlayerIdentityId);

    return new ApiTokens(false, apiToken);
  }

  /// <summary>
  /// Generate API token using JWT format. This token is expected to be used during Game API authentication.
  /// </summary>
  /// <param name="providerName"></param>
  /// <param name="providerIdentityId"></param>
  /// <param name="playerId"></param>
  /// <param name="playerIdentityId"></param>
  /// <returns></returns>
  public virtual string GenerateApiJwtToken(string providerName, string providerIdentityId, long playerId, long playerIdentityId)
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
}
