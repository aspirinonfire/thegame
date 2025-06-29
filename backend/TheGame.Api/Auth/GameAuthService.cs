using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using MediatR;
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
using TheGame.Api.CommandHandlers;
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
  
  public const string MissingAuthCodeError = "missing_auth_code";
  public const string ErrorWhileExchangingAuthCodeForTokens = "auth_code_exchange_error";
  public const string MissingIdTokenError = "missing_id_token";
  public const string InvalidIdTokenError = "invalid_id_token";
  public const string GeneralErrorWhileValidatingTokenError = "token_validation_general_error";
  public const string InvalidRefreshParameters = "access_refresh_parameters_invalid";

  /// <summary>
  /// Use Google Authorization code to obtain player identity and generate API auth and refresh tokens.
  /// </summary>
  /// <remarks>
  /// This method expects Authorization Code which is exchanged for ID Token since its payload contains everything required to generate valid player identity.
  /// Currently, no additional user information is required so Access Token is not needed.
  /// </remarks>
  /// <param name="googleAuthCode"></param>
  /// <param name="httpContext"></param>
  /// <returns></returns>
  public async Task<Result<ApiTokens>> AuthenticateWithGoogleAuthCode(string googleAuthCode, HttpContext httpContext)
  {
    var tokenResult = await ExchangeGoogleAuthCodeForTokens(googleAuthCode);
    if (!tokenResult.TryGetSuccessful(out var googleTokens, out var tokenFailure))
    {
      return tokenFailure;
    }

    var googleIdentityResult = await GetValidatedGoogleIdTokenPayload(googleTokens.IdToken);
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
      logger.LogError(ex, "Failed to validate access token.");
      return new Failure(InvalidRefreshParameters);
    }

    if (playerId < 1)
    {
      logger.LogError("PlayerId claim value is invalid.");
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

  public virtual async Task<Result<TokenResponse>> ExchangeGoogleAuthCodeForTokens(string authCode)
  {
    if (string.IsNullOrWhiteSpace(authCode))
    {
      return new Failure(MissingAuthCodeError);
    }

    using var authCodeFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer()
    {
      ClientSecrets = new Google.Apis.Auth.OAuth2.ClientSecrets
      {
        ClientId = gameSettings.Value.Auth.Google.ClientId,
        ClientSecret = gameSettings.Value.Auth.Google.ClientSecret
      },
      DataStore = new NoopDataStore()
    });

    try
    {
      var tokenResponse = await authCodeFlow.ExchangeCodeForTokenAsync("userId",
      authCode,
      "postmessage",
      CancellationToken.None);

      if (tokenResponse != null)
      {
        return tokenResponse;
      }

      logger.LogError("Failed to exchange auth code for google tokens. Got empty response.");
      return new Failure(ErrorWhileExchangingAuthCodeForTokens);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to exchange auth code for google tokens.");
      return new Failure(ErrorWhileExchangingAuthCodeForTokens);
    }
  }

  /// <summary>
  /// Validate Google ID Token and return payload claims. For validation rules see <see href="https://developers.google.com/identity/openid-connect/openid-connect#validatinganidtoken"/>
  /// </summary>
  /// <param name="googleIdToken"></param>
  /// <returns></returns>
  public virtual async Task<Result<GoogleJsonWebSignature.Payload>> GetValidatedGoogleIdTokenPayload(string googleIdToken)
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
      logger.LogError(ex, "Encountered error while validating Google ID Token.");
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
      expires: DateTime.UtcNow.AddMinutes(gameSettings.Value.Auth.Api.JwtTokenExpirationMin),
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

  private sealed class NoopDataStore : IDataStore
  {
    public Task ClearAsync() => Task.CompletedTask;

    public Task DeleteAsync<T>(string key) => Task.CompletedTask;

    public Task<T> GetAsync<T>(string key) => Task.FromResult<T>(default!);

    public Task StoreAsync<T>(string key, T value) => Task.CompletedTask;
  }
}
