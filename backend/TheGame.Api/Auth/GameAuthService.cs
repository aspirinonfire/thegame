using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public sealed record ApiTokens(bool IsNewIdentity, string AccessToken);

public sealed record ApiAccessTokenPayload(long PlayerId, string PlayerIdentityName, string PlayerIdentityId, string RefreshTokenId, bool IsExpired);

public sealed record RefreshTokenPayload([JsonProperty("rtid")] string RefreshTokenId,
  [JsonProperty("n")] string Nonce,
  [JsonProperty("e")] long ExpiresIn);

public sealed record NewRefreshToken(string RefreshTokenId, string RefreshTokenValue, long ExpireUnixSeconds);

public interface IGameAuthService
{
  Result<RefreshTokenPayload> ExtractRefreshTokenPayload(string refreshTokenValue);
  string GenerateApiJwtToken(string providerName, string providerIdentityId, long playerId, long playerIdentityId, string refreshTokenId);
  Result<NewRefreshToken> GenerateRefreshToken();
  Result<ApiAccessTokenPayload> GetAccessTokenPayload(string accessToken);
  string RetrieveRefreshTokenValue(HttpContext httpContext);
  void SetRefreshCookie(HttpContext httpContext, string refreshTokenValue, TimeSpan tokenExpiration);
}

public class GameAuthService(ICryptoHelper cryptoHelper,
  TimeProvider timeProvider,
  ILogger<GameAuthService> logger,
  IOptions<GameSettings> gameSettings) : IGameAuthService
{
  public const string JwtSigKeyInfo = "jwt-sig";
  public const string RefreshTokenKeyInfo = "refresh-token";

  public static SymmetricSecurityKey GetAccessTokenSigningKey(string jwtSecret)
  {
    var key = CryptoHelper.DeriveHkdfKey(jwtSecret, JwtSigKeyInfo, 32);
    return new SymmetricSecurityKey(key.ToArray());
  } 

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

  public static IReadOnlyCollection<Claim> CreateAccessTokenClaims(string providerName,
    string providerIdentityId,
    long playerId,
    long playerIdentityId,
    string refreshTokenId)
  {
    return [
      new(PlayerIdentityAuthority, providerName, ClaimValueTypes.String),
      new(PlayerIdentityUserId, providerIdentityId, ClaimValueTypes.String),
      new(PlayerIdClaimType, $"{playerId}", ClaimValueTypes.String),
      new(PlayerIdentityIdClaimType, $"{playerIdentityId}", ClaimValueTypes.String),
      new(RefreshTokenId, refreshTokenId, ClaimValueTypes.String)
    ];
  }

  public const string ApiRefreshTokenCookieName = "gameapi.refresh";

  public const string PlayerIdentityAuthority = "iden_authority";
  public const string PlayerIdentityUserId = "iden_user_id";
  public const string PlayerIdentityIdClaimType = "player_identiy_id";
  public const string PlayerIdClaimType = "player_id";
  public const string RefreshTokenId = "refresh_id";

  public const string InvalidRefreshParameters = "access_refresh_parameters_invalid";  

  /// <summary>
  /// Generate API token using JWT format. This token is expected to be used during Game API authentication.
  /// </summary>
  /// <param name="providerName"></param>
  /// <param name="providerIdentityId"></param>
  /// <param name="playerId"></param>
  /// <param name="playerIdentityId"></param>
  /// <returns></returns>
  public string GenerateApiJwtToken(string providerName, string providerIdentityId, long playerId, long playerIdentityId, string refreshTokenId)
  {
    var claims = CreateAccessTokenClaims(providerName, providerIdentityId, playerId, playerIdentityId, refreshTokenId);

    var jwtToken = new JwtSecurityToken(
      claims: claims,
      notBefore: DateTime.UtcNow,
      expires: DateTime.UtcNow.AddMinutes(gameSettings.Value.Auth.Api.JwtTokenExpirationMin),
      issuer: gameSettings.Value.Auth.Api.JwtAudience,
      audience: gameSettings.Value.Auth.Api.JwtAudience,
      signingCredentials: new SigningCredentials(
        GetAccessTokenSigningKey(gameSettings.Value.Auth.Api.JwtSecret),
        SecurityAlgorithms.HmacSha256Signature)
      );

    return new JwtSecurityTokenHandler().WriteToken(jwtToken);
  }

  public Result<ApiAccessTokenPayload> GetAccessTokenPayload(string accessToken)
  {
    long playerId = 0;
    string? identityProvider;
    string? identityId;
    string? refreshTokenId;
    bool isExpired = true;
    try
    {
      // validate token
      var jwtValidationParams = GetTokenValidationParams(gameSettings.Value.Auth.Api.JwtAudience,
        gameSettings.Value.Auth.Api.JwtSecret,
        gameSettings.Value.Auth.Api.JwtAudience);

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

      refreshTokenId = tokenClaims.FindFirstValue(RefreshTokenId);

      var expiration = tokenClaims.FindFirstValue(ClaimTypes.Expiration);
      if (!string.IsNullOrWhiteSpace(expiration) && long.TryParse(expiration, out var unixSeconds))
      {
        isExpired = TimeProvider.System.GetUtcNow() > DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
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

    return new ApiAccessTokenPayload(playerId,
      identityProvider,
      identityId,
      refreshTokenId ?? string.Empty,
      isExpired);
  }

  public Result<NewRefreshToken> GenerateRefreshToken()
  { 
    var expiresIn = timeProvider
      .GetUtcNow()
      .AddMinutes(gameSettings.Value.Auth.Api.RefreshTokenAgeMinutes)
      .ToUnixTimeSeconds();

    var refreshTokenPayload = new RefreshTokenPayload(Guid.NewGuid().ToString("N"),
      Convert.ToHexString(RandomNumberGenerator.GetBytes(16)),
      expiresIn);

    var encryptedTokenValueResult = cryptoHelper.EncryptPayload(refreshTokenPayload,
      gameSettings.Value.Auth.Api.JwtSecret,
      RefreshTokenKeyInfo);

    if (encryptedTokenValueResult.TryGetSuccessful(out var encryptedTokenValue, out var encryptionFailure))
    {
      return new NewRefreshToken(refreshTokenPayload.RefreshTokenId, encryptedTokenValue, expiresIn);
    }
    
    return encryptionFailure;
  }

  public Result<RefreshTokenPayload> ExtractRefreshTokenPayload(string refreshTokenValue)
  {
    var decryptedTokenResult = cryptoHelper.DecryptPayload<RefreshTokenPayload>(refreshTokenValue,
      gameSettings.Value.Auth.Api.JwtSecret,
      RefreshTokenKeyInfo);

    if (decryptedTokenResult.TryGetSuccessful(out var decryptedToken, out var decryptionFailure))
    {
      return decryptedToken;
    }

    return decryptionFailure;
  }

  public void SetRefreshCookie(HttpContext httpContext, string refreshTokenValue, TimeSpan tokenExpiration)
  {
    httpContext.Response.Cookies.Append(ApiRefreshTokenCookieName,
      refreshTokenValue,
      new CookieOptions()
      {
        HttpOnly = true,
        IsEssential = true,
        SameSite = SameSiteMode.None,
        Secure = true,
        MaxAge = tokenExpiration
      });
  }

  public string RetrieveRefreshTokenValue(HttpContext httpContext) => httpContext.Request.Cookies
    .Where(cookie => cookie.Key == ApiRefreshTokenCookieName)
    .Select(cookie => cookie.Value)
    .FirstOrDefault(string.Empty);
}
