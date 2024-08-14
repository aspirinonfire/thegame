using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneOf;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public sealed record ApiTokens(bool IsNewIdentity, string AccessToken);

public class GameAuthService(ILogger<GameAuthService> logger, IMediator mediatr, ISystemService systemService, IOptions<GameSettings> gameSettings)
{
  public static SymmetricSecurityKey GetAccessTokenSigningKey(string jwtSecret) =>
    new (Encoding.UTF8.GetBytes(jwtSecret));

  public static TokenValidationParameters GetTokenValidationParams(string jwtAudience, string jwtSecret) =>
    new ()
    {
      ValidateIssuer = false,
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
  
  public const string MissingIdTokenError = "missing_id_token";
  public const string InvalidIdTokenError = "invalid_id_token";
  public const string GeneralErrorWhileValidatingTokenError = "token_validation_general_error";
  public const string InvalidRefreshParameters = "access_refresh_parameters_invalid";

  /// <summary>
  /// Use valid Google ID Token to obtain player identity and generate API auth and refresh tokens.
  /// </summary>
  /// <remarks>
  /// This method expects ID Token since its payload contains everything required to generate valid player identity.
  /// Currently, no additional user information is required so Access Token is not needed.
  /// If that requirement changes, UI will need to provide authorization code so auth method can obtain necessary tokens and make appropriate requests.
  /// </remarks>
  /// <param name="googleIdToken"></param>
  /// <param name="httpContext"></param>
  /// <returns></returns>
  public async Task<OneOf<ApiTokens, Failure>> AuthenticateWithGoogleIdToken(string googleIdToken, HttpContext httpContext)
  {
    var googleIdentityResult = await GetValidatedGoogleIdTokenPayload(googleIdToken);
    if (!googleIdentityResult.TryGetSuccessful(out var idTokenPayload, out var tokenValidationFailure))
    {
      return tokenValidationFailure;
    }

    var identityRequest = new NewPlayerIdentityRequest("Google",
      idTokenPayload.Subject,
      idTokenPayload.Name,
      gameSettings.Value.Auth.Api.RefreshTokenByteCount,
      gameSettings.Value.Auth.Api.RefreshTokenAgeMinutes);

    var getOrCreatePlayerCommand = new GetOrCreateNewPlayerCommand(identityRequest);
    var getOrCreatePlayerResult = await mediatr.Send(getOrCreatePlayerCommand);
    if (!getOrCreatePlayerResult.TryGetSuccessful(out var playerIdentity, out var commandFailure))
    {
      return commandFailure;
    }

    if (!string.IsNullOrEmpty(playerIdentity.RefreshToken) &&
      playerIdentity.RefreshTokenExpiration.HasValue)
    {
      var cookieExpiration = playerIdentity.RefreshTokenExpiration.Value - systemService.DateTimeOffset.UtcNow;

      SetRefreshCookie(httpContext, playerIdentity.RefreshToken, cookieExpiration);
    }
    
    var apiToken = GenerateApiJwtToken(playerIdentity.ProviderName,
      playerIdentity.ProviderIdentityId,
      playerIdentity.PlayerId,
      playerIdentity.PlayerIdentityId);

    return new ApiTokens(playerIdentity.IsNewIdentity, apiToken);
  }

  public virtual async Task<OneOf<ApiTokens, Failure>> RefreshAccessToken(HttpContext httpContext, string accessToken)
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
        gameSettings.Value.Auth.Api.JwtSecret);
      
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
      logger.LogError(ex, "Failed to validate access token");
      return new Failure(InvalidRefreshParameters);
    }

    if (playerId < 1)
    {
      logger.LogError("PlayerId claim value is invalid");
      return new Failure(InvalidRefreshParameters);
    }

    var refreshTokenResult = await mediatr.Send(new RotatePlayerIdentityRefreshTokenCommand(playerId,
      refreshCookieValue,
      gameSettings.Value.Auth.Api.RefreshTokenByteCount,
      gameSettings.Value.Auth.Api.RefreshTokenAgeMinutes));
    if (!refreshTokenResult.TryGetSuccessful(out var newRefreshToken, out var refreshFailure))
    {
      return refreshFailure;
    }

    var cookieExpiration = newRefreshToken.RefreshTokenExpiration - systemService.DateTimeOffset.UtcNow;

    SetRefreshCookie(httpContext, newRefreshToken.RefreshToken, cookieExpiration);

    var apiToken = GenerateApiJwtToken(newRefreshToken.ProviderName,
      newRefreshToken.ProviderIdentityId,
      newRefreshToken.PlayerId,
      newRefreshToken.PlayerIdentityId);

    return new ApiTokens(false, apiToken);
  }

  /// <summary>
  /// Validate Google ID Token and return payload claims. For validation rules see <see href="https://developers.google.com/identity/openid-connect/openid-connect#validatinganidtoken"/>
  /// </summary>
  /// <param name="googleIdToken"></param>
  /// <returns></returns>
  public virtual async Task<OneOf<GoogleJsonWebSignature.Payload, Failure>> GetValidatedGoogleIdTokenPayload(string googleIdToken)
  {
    if (string.IsNullOrEmpty(googleIdToken))
    {
      return new Failure(MissingIdTokenError);
    }

    try
    {
      var tokenValidationSettings = new GoogleJsonWebSignature.ValidationSettings()
      {
        Audience = [gameSettings.Value.Auth.Google.ClientId]
      };

      return await GoogleJsonWebSignature.ValidateAsync(googleIdToken, tokenValidationSettings);
    }
    catch (InvalidJwtException invalidIdTokenException)
    {
      logger.LogError(invalidIdTokenException, "Google ID Token is invalid.");
      return new Failure(InvalidIdTokenError);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Encountered error while validating Google ID Token");
      return new Failure(GeneralErrorWhileValidatingTokenError);
    }
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
      expires: DateTime.UtcNow.AddMinutes(60),
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
