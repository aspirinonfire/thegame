using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.Utils;

namespace TheGame.Api.CommandHandlers;

public sealed record GetOrCreateNewPlayerCommand(NewPlayerIdentityRequest NewPlayerIdentityRequest)
{
  public sealed record Result(bool IsNewIdentity,
    long PlayerIdentityId,
    long PlayerId,
    string ProviderName,
    string ProviderIdentityId,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiration);
}

public sealed class GetOrCreateNewPlayerCommandHandler(IGameDbContext gameDb,
  IPlayerIdentityFactory playerIdentityFactory,
  ITransactionExecutionWrapper transactionWrapper,
  TimeProvider timeProvider,
  ILogger<GetOrCreateNewPlayerCommand> logger)
    : ICommandHandler<GetOrCreateNewPlayerCommand, GetOrCreateNewPlayerCommand.Result>
{
    public async Task<Result<GetOrCreateNewPlayerCommand.Result>> Execute(GetOrCreateNewPlayerCommand command, CancellationToken cancellationToken) =>
      await transactionWrapper.ExecuteInTransaction<GetOrCreateNewPlayerCommand.Result>(
          async () =>
          {
            var playerIdentity = await gameDb.PlayerIdentities
              .Include(ident => ident.Player)
              .Where(ident =>
                ident.ProviderName == command.NewPlayerIdentityRequest.ProviderName &&
                ident.ProviderIdentityId == command.NewPlayerIdentityRequest.ProviderIdentityId)
              .FirstOrDefaultAsync(cancellationToken);

            if (playerIdentity == null)
            {
              logger.LogInformation("Attempting to create new player with identity.");

              var newIdentityResult = playerIdentityFactory.CreatePlayerIdentity(command.NewPlayerIdentityRequest);
              if (!newIdentityResult.TryGetSuccessful(out playerIdentity, out var failure))
              {
                logger.LogError(failure.GetException(), "New player cannot be created.");
                return failure;
              }

              logger.LogInformation("New player with identity was created successfully.");
            }
            else
            {
              logger.LogInformation("Found an existing player with identity.");
            }

            var isMissingRefreshToken = string.IsNullOrWhiteSpace(playerIdentity.RefreshToken);
            var tokenExpiredOrAboutToExpire = playerIdentity.RefreshTokenExpiration.GetValueOrDefault().Add(TimeSpan.FromMinutes(5)) <= timeProvider.GetUtcNow();

            if (isMissingRefreshToken || tokenExpiredOrAboutToExpire)
            {
              logger.LogInformation("Refresh token needs rotation.");
              var tokenRefreshResult = playerIdentity.RotateRefreshToken(timeProvider,
                command.NewPlayerIdentityRequest.RefreshTokenByteCount,
                TimeSpan.FromMinutes(command.NewPlayerIdentityRequest.RefreshTokenAgeMinutes));

              if (!tokenRefreshResult.TryGetSuccessful(out _, out var refreshFailure))
              {
                logger.LogError(refreshFailure.GetException(), "Failed to renew refresh token.");
                return refreshFailure;
              }
            }

            await gameDb.SaveChangesAsync(cancellationToken);

            return new GetOrCreateNewPlayerCommand.Result(false,
                playerIdentity.Id,
                playerIdentity.Player!.Id,
                playerIdentity.ProviderName,
                playerIdentity.ProviderIdentityId,
                playerIdentity.RefreshToken,
                playerIdentity.RefreshTokenExpiration);
          },
          nameof(GetOrCreateNewPlayerCommand),
          logger,
          cancellationToken);
}
