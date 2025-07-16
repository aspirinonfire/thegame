using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
  IOptions<GameSettings> gameSettings,
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
          logger.LogError("Refresh or Access token are missing.");
          return new ValidationFailure(nameof(command), "Refresh and Access Tokens are required!");
        }

        var validAccessTokenResult = gameAuthService.GetValidateExpiredAccessToken(command.AccessToken);
        if (!validAccessTokenResult.TryGetSuccessful(out var accessTokenValues, out var tokenValidationFailure))
        {
          logger.LogError(tokenValidationFailure.GetException(), "Access token is invalid.");
          return tokenValidationFailure;
        }

        var currentTimestamp = timeProvider.GetUtcNow();

        var playerIdentity = await gameDb.PlayerIdentities
          .Include(ident => ident.Player)
          .Where(ident =>
            ident.Player.Id == accessTokenValues.PlayerId &&
            ident.ProviderName == accessTokenValues.PlayerIdentityName &&
            ident.ProviderIdentityId == accessTokenValues.PlayerIdentityId &&
            ident.RefreshToken == command.RefreshToken &&
            ident.RefreshTokenExpiration > currentTimestamp)
          .FirstOrDefaultAsync(cancellationToken);

        if (playerIdentity == null)
        {
          logger.LogError("User Identity not found or refresh token has expired");
          return new Failure("Failed to find player identity");
        }

        var tokenRefreshResult = playerIdentity.RotateRefreshToken(timeProvider,
          gameSettings.Value.Auth.Api.RefreshTokenByteCount,
          TimeSpan.FromMinutes(gameSettings.Value.Auth.Api.RefreshTokenAgeMinutes));

        if (!tokenRefreshResult.TryGetSuccessful(out _, out var refreshFailure))
        {
          logger.LogError(refreshFailure.GetException(), "Failed to renew refresh token.");
          return refreshFailure;
        }

        await gameDb.SaveChangesAsync(cancellationToken);

        var apiToken = gameAuthService.GenerateApiJwtToken(playerIdentity.ProviderName,
          playerIdentity.ProviderIdentityId,
          playerIdentity.Player.Id,
          playerIdentity.Player.PlayerIdentityId.GetValueOrDefault());

        var refreshExpiresIn = playerIdentity.RefreshTokenExpiration.GetValueOrDefault() - currentTimestamp;

        return new RefreshAccessTokenCommand.Result(apiToken,
          playerIdentity.RefreshToken!,
          refreshExpiresIn);
      },
      nameof(RefreshAccessTokenCommand),
      logger,
      cancellationToken);
}
