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
public sealed record AuthenticateWithAuthCodeCommand(string AuthCode)
{
  public sealed record Result(bool IsNewIdentity, string AccessToken, string RefreshTokenValue, TimeSpan RefreshTokenExpiresIn);
}

public class AuthenticateWithAuthCodeCommandHandler(IGameAuthService gameAuthService,
  IPlayerService playerService,
  TimeProvider timeProvider,
  IGoogleAuthService googleAuthService,
  ITransactionExecutionWrapper transactionWrapper,
  ILogger<AuthenticateWithAuthCodeCommandHandler> logger)
  : ICommandHandler<AuthenticateWithAuthCodeCommand, AuthenticateWithAuthCodeCommand.Result>
{
  public async Task<Result<AuthenticateWithAuthCodeCommand.Result>> Execute(AuthenticateWithAuthCodeCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<AuthenticateWithAuthCodeCommand.Result>(
      async () =>
      {
        var googleTokenResult = await googleAuthService.ExchangeGoogleAuthCodeForTokens(command.AuthCode, cancellationToken);
        if (!googleTokenResult.TryGetSuccessful(out var token, out var authCodeFailure))
        {
          return authCodeFailure;
        }

        var googleIdentityResult = await googleAuthService.GetValidatedGoogleIdTokenPayload(token.IdToken);
        if (!googleIdentityResult.TryGetSuccessful(out var idTokenPayload, out var tokenValidationFailure))
        {
          return tokenValidationFailure;
        }

        var identityRequest = new NewPlayerIdentityRequest("Google",
          idTokenPayload.Subject,
          idTokenPayload.Name);

        var getOrCreatePlayerCommand = new GetOrCreatePlayerRequest(identityRequest);
        var getOrCreatePlayerResult = await playerService.GetOrCreatePlayer(getOrCreatePlayerCommand, cancellationToken);
        if (!getOrCreatePlayerResult.TryGetSuccessful(out var playerIdentity, out var createPlayerFailure))
        {
          return createPlayerFailure;
        }

        var newRefreshTokenResult = gameAuthService.GenerateRefreshToken();
        if (!newRefreshTokenResult.TryGetSuccessful(out var refreshToken, out var refreshTokenFailure))
        {
          return refreshTokenFailure;
        }

        var refreshTokenExpiresIn = DateTimeOffset.FromUnixTimeSeconds(refreshToken.ExpireUnixSeconds) - timeProvider.GetUtcNow();

        var apiToken = gameAuthService.GenerateApiJwtToken(playerIdentity.ProviderName,
          playerIdentity.ProviderIdentityId,
          playerIdentity.PlayerId,
          playerIdentity.PlayerIdentityId,
          refreshToken.RefreshTokenId);

        return new AuthenticateWithAuthCodeCommand.Result(playerIdentity.IsNewIdentity,
          apiToken,
          refreshToken.RefreshTokenValue,
          refreshTokenExpiresIn);
      },
      nameof(AuthenticateWithAuthCodeCommand),
      logger,
      cancellationToken);
}
