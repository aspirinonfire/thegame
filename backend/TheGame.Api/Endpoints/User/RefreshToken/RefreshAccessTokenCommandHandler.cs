using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Api.Auth;
using TheGame.Api.Common;
using TheGame.Domain.DomainModels;
using TheGame.Domain.Utils;

namespace TheGame.Api.Endpoints.User.RefreshToken;

public sealed record RefreshAccessTokenCommand(string AccessToken,
  string RefreshToken)
{
  public sealed record Result(string AccessToken, string RefreshTokenValue, TimeSpan RefreshTokenExpiresIn);
}

public class RefreshAccessTokenCommandHandler(IGameDbContext gameDb,
  IGameAuthService gameAuthService,
  TimeProvider timeProvider,
  ITransactionExecutionWrapper transactionWrapper,
  ILogger<RefreshAccessTokenCommandHandler> logger)
  : ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenCommand.Result>
{
  public async Task<Result<RefreshAccessTokenCommand.Result>> Execute(RefreshAccessTokenCommand command, CancellationToken cancellationToken) => await
    transactionWrapper.ExecuteInTransaction<RefreshAccessTokenCommand.Result>(
      async () =>
      {
        if (string.IsNullOrEmpty(command.AccessToken) || string.IsNullOrEmpty(command.RefreshToken))
        {
          logger.LogError("Refresh or Id token is missing.");
          return new ValidationFailure(nameof(command), "Refresh and Id Tokens are required!");
        }

        var validAccessTokenResult = gameAuthService.GetAccessTokenPayload(command.AccessToken);
        if (!validAccessTokenResult.TryGetSuccessful(out var accessTokenPayload, out var tokenValidationFailure))
        {
          logger.LogError(tokenValidationFailure.GetException(), "Access token is invalid.");
          return tokenValidationFailure;
        }

        var refreshTokenPayloadResult = gameAuthService.ExtractRefreshTokenPayload(command.RefreshToken);
        if (!refreshTokenPayloadResult.TryGetSuccessful(out var refreshTokenPayload, out var currentRefreshTokenFailure))
        {
          logger.LogError(currentRefreshTokenFailure.GetException(), "Failed to extract refresh token payload.");
          return currentRefreshTokenFailure;
        }

        var refreshExpired = timeProvider.GetUtcNow() > DateTimeOffset.FromUnixTimeSeconds(refreshTokenPayload.ExpiresIn);
        if (refreshExpired)
        {
          logger.LogError("Refresh token expired.");
          return new Failure("Invalid Access Or Refresh Token.");
        }

        if (accessTokenPayload.RefreshTokenId != refreshTokenPayload.RefreshTokenId)
        {
          logger.LogError("Refresh token ID mismatch.");
          return new Failure("Invalid Access Or Refresh Token.");
        }

        var currentTimestamp = timeProvider.GetUtcNow();

        var playerIdentity = await gameDb.PlayerIdentities
          .Include(ident => ident.Player)
          .Where(ident =>
            !ident.IsDisabled &&
            ident.Player.Id == accessTokenPayload.PlayerId &&
            ident.ProviderName == accessTokenPayload.PlayerIdentityName &&
            ident.ProviderIdentityId == accessTokenPayload.PlayerIdentityId)
          .FirstOrDefaultAsync(cancellationToken);

        if (playerIdentity == null)
        {
          logger.LogError("User Identity not found or is disabled");
          return new Failure("Failed to find player identity");
        }

        await gameDb.SaveChangesAsync(cancellationToken);

        var newRefreshTokenResult = gameAuthService.GenerateRefreshToken();
        if (!newRefreshTokenResult.TryGetSuccessful(out var refreshToken, out var newRefreshTokenFailure))
        {
          return newRefreshTokenFailure;
        }

        var refreshExpiresIn = DateTimeOffset.FromUnixTimeSeconds(refreshToken.ExpireUnixSeconds) - timeProvider.GetUtcNow();

        var apiToken = gameAuthService.GenerateApiJwtToken(playerIdentity.ProviderName,
          playerIdentity.ProviderIdentityId,
          playerIdentity.Player.Id,
          playerIdentity.Player.PlayerIdentityId.GetValueOrDefault(),
          refreshToken.RefreshTokenId);

        return new RefreshAccessTokenCommand.Result(apiToken,
          refreshToken.RefreshTokenValue,
          refreshExpiresIn);
      },
      nameof(RefreshAccessTokenCommand),
      logger,
      cancellationToken);
}
