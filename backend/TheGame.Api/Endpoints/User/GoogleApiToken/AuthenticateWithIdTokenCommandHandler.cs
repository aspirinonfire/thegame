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
public sealed record AuthenticateWithIdTokenCommand(string IdToken)
{
  public sealed record Result(bool IsNewIdentity, string AccessToken, string RefreshTokenValue, TimeSpan RefreshTokenExpiresIn);
}

public class AuthenticateWithIdTokenCommandHandler(IGameAuthService gameAuthService,
  IPlayerService playerService,
  TimeProvider timeProvider,
  IOptions<GameSettings> gameSettings,
  IGoogleAuthService googleAuthService,
  ITransactionExecutionWrapper transactionWrapper,
  ILogger<AuthenticateWithIdTokenCommandHandler> logger)
  : ICommandHandler<AuthenticateWithIdTokenCommand, AuthenticateWithIdTokenCommand.Result>
{
  public async Task<Result<AuthenticateWithIdTokenCommand.Result>> Execute(AuthenticateWithIdTokenCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<AuthenticateWithIdTokenCommand.Result>(
      async () =>
      {
        var googleIdentityResult = await googleAuthService.GetValidatedGoogleIdTokenPayload(command.IdToken);
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

        return new AuthenticateWithIdTokenCommand.Result(playerIdentity.IsNewIdentity,
          apiToken,
          playerIdentity.RefreshToken,
          refreshTokenExpiresIn);
      },
      nameof(AuthenticateWithIdTokenCommand),
      logger,
      cancellationToken);
}
