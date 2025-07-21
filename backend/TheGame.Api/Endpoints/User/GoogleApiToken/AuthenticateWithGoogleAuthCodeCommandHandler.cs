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
  IGoogleAuthService googleAuthService,
  ITransactionExecutionWrapper transactionWrapper,
  ILogger<AuthenticateWithGoogleAuthCodeCommandHandler> logger)
  : ICommandHandler<AuthenticateWithGoogleAuthCodeCommand, AuthenticateWithGoogleAuthCodeCommand.Result>
{
  public async Task<Result<AuthenticateWithGoogleAuthCodeCommand.Result>> Execute(AuthenticateWithGoogleAuthCodeCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<AuthenticateWithGoogleAuthCodeCommand.Result>(
      async () =>
      {
        var tokenResult = await googleAuthService.ExchangeGoogleAuthCodeForTokens(command.AuthCode, cancellationToken);
        if (!tokenResult.TryGetSuccessful(out var googleTokens, out var tokenFailure))
        {
          return tokenFailure;
        }

        var googleIdentityResult = await googleAuthService.GetValidatedGoogleIdTokenPayload(googleTokens.IdToken);
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
}
