using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public interface IGoogleAuthService
{
  /// <summary>
  /// Exchange Google Auth Code for Game API access token
  /// </summary>
  /// <param name="authCode"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task<Result<TokenResponse>> ExchangeGoogleAuthCodeForTokens(string authCode, CancellationToken cancellationToken);

  /// <summary>
  /// Validate Google ID Token and return payload claims. For validation rules see <see href="https://developers.google.com/identity/openid-connect/openid-connect#validatinganidtoken"/>
  /// </summary>
  /// <param name="googleIdToken"></param>
  /// <returns></returns>
  Task<Result<GoogleJsonWebSignature.Payload>> GetValidatedGoogleIdTokenPayload(string googleIdToken);
}

public sealed class GoogleAuthService(IOptions<GameSettings> gameSettings,
  ILogger<GoogleAuthService> logger) : IGoogleAuthService
{
  public const string MissingAuthCodeError = "missing_auth_code";
  public const string ErrorWhileExchangingAuthCodeForTokens = "auth_code_exchange_error";
  public const string MissingIdTokenError = "missing_id_token";
  public const string InvalidIdTokenError = "invalid_id_token";
  public const string GeneralErrorWhileValidatingTokenError = "token_validation_general_error";

  public async Task<Result<TokenResponse>> ExchangeGoogleAuthCodeForTokens(string authCode, CancellationToken cancellationToken)
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
      cancellationToken);

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

  public async Task<Result<GoogleJsonWebSignature.Payload>> GetValidatedGoogleIdTokenPayload(string googleIdToken)
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

  private sealed class NoopDataStore : IDataStore
  {
    public Task ClearAsync() => Task.CompletedTask;

    public Task DeleteAsync<T>(string key) => Task.CompletedTask;

    public Task<T> GetAsync<T>(string key) => Task.FromResult<T>(default!);

    public Task StoreAsync<T>(string key, T value) => Task.CompletedTask;
  }
}