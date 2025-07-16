using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Api.Auth;
using TheGame.Api.Common;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.Utils;

namespace TheGame.Api.Endpoints.User.GoogleApiToken;

/// <summary>
/// Use Google Authorization code to obtain player identity and generate API auth and refresh tokens.
/// </summary>
/// <remarks>
/// This method expects Authorization Code which is exchanged for ID Token since its payload contains everything required to generate valid player identity.
/// Currently, no additional user information is required so Access Token is not needed.
/// </remarks>
public sealed record AuthenticateWithGoogleAuthCodeCommand(string AuthCode)
{
  public sealed record Result(bool IsNewIdentity, string AccessToken, string RefreshTokenValue, TimeSpan RefreshTokenExpiresIn);
}

public class AuthenticateWithGoogleAuthCodeCommandHandler(IGameAuthService gameAuthService,
  IPlayerService playerService,
  TimeProvider timeProvider,
  IOptions<GameSettings> gameSettings,
  ITransactionExecutionWrapper transactionWrapper,
  ILogger<AuthenticateWithGoogleAuthCodeCommandHandler> logger)
  : ICommandHandler<AuthenticateWithGoogleAuthCodeCommand, AuthenticateWithGoogleAuthCodeCommand.Result>
{
  public const string MissingAuthCodeError = "missing_auth_code";
  public const string ErrorWhileExchangingAuthCodeForTokens = "auth_code_exchange_error";
  public const string MissingIdTokenError = "missing_id_token";
  public const string InvalidIdTokenError = "invalid_id_token";
  public const string GeneralErrorWhileValidatingTokenError = "token_validation_general_error";

  public async Task<Result<AuthenticateWithGoogleAuthCodeCommand.Result>> Execute(AuthenticateWithGoogleAuthCodeCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<AuthenticateWithGoogleAuthCodeCommand.Result>(
      async () =>
      {
        var tokenResult = await ExchangeGoogleAuthCodeForTokens(command.AuthCode, cancellationToken);
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

        var getOrCreatePlayerCommand = new GetOrCreatePlayerRequest(identityRequest);
        var getOrCreatePlayerResult = await playerService.GetOrCreatePlayer(getOrCreatePlayerCommand, cancellationToken);
        if (!getOrCreatePlayerResult.TryGetSuccessful(out var playerIdentity, out var commandFailure))
        {
          return commandFailure;
        }

        var apiToken = gameAuthService.GenerateApiJwtToken(playerIdentity.ProviderName,
          playerIdentity.ProviderIdentityId,
          playerIdentity.PlayerId,
          playerIdentity.PlayerIdentityId);

        var refreshTokenExpiresIn = playerIdentity.RefreshTokenExpiration - timeProvider.GetUtcNow();

        return new AuthenticateWithGoogleAuthCodeCommand.Result(playerIdentity.IsNewIdentity,
          apiToken,
          playerIdentity.RefreshToken,
          refreshTokenExpiresIn);
      },
      nameof(AuthenticateWithGoogleAuthCodeCommand),
      logger,
      cancellationToken);

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

  /// <summary>
  /// Validate Google ID Token and return payload claims. For validation rules see <see href="https://developers.google.com/identity/openid-connect/openid-connect#validatinganidtoken"/>
  /// </summary>
  /// <param name="googleIdToken"></param>
  /// <returns></returns>
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
