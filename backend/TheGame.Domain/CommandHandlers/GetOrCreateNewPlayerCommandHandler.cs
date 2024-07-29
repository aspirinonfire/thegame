using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.CommandHandlers;

public sealed record GetOrCreateNewPlayerCommand(NewPlayerIdentityRequest NewPlayerIdentityRequest) : IRequest<Maybe<GetOrCreatePlayerResult>>;

public sealed record GetOrCreatePlayerResult(bool IsNewIdentity,
  long PlayerIdentityId,
  long PlayerId,
  string ProviderName,
  string ProviderIdentityId,
  string? RefreshToken,
  DateTimeOffset? RefreshTokenExpiration);

public sealed class GetOrCreateNewPlayerCommandHandler(IGameDbContext gameDb,
  IPlayerIdentityFactory playerIdentityFactory,
  ITransactionExecutionWrapper transactionWrapper,
  ISystemService systemService,
  ILogger<GetOrCreateNewPlayerCommand> logger)
  : IRequestHandler<GetOrCreateNewPlayerCommand, Maybe<GetOrCreatePlayerResult>>
{
    public async Task<Maybe<GetOrCreatePlayerResult>> Handle(GetOrCreateNewPlayerCommand request, CancellationToken cancellationToken) =>
      await transactionWrapper.ExecuteInTransaction<GetOrCreatePlayerResult>(
          async () =>
          {
            var playerIdentity = await gameDb.PlayerIdentities
              .Include(ident => ident.Player)
              .Where(ident =>
                ident.ProviderName == request.NewPlayerIdentityRequest.ProviderName &&
                ident.ProviderIdentityId == request.NewPlayerIdentityRequest.ProviderIdentityId)
              .FirstOrDefaultAsync(cancellationToken);

            if (playerIdentity == null)
            {
              logger.LogInformation("Attempting to create new player with identity.");

              var newIdentityResult = playerIdentityFactory.CreatePlayerIdentity(request.NewPlayerIdentityRequest);
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
            var tokenExpiredOrAboutToExpire = playerIdentity.RefreshTokenExpiration.GetValueOrDefault().Add(TimeSpan.FromMinutes(5)) <= systemService.DateTimeOffset.UtcNow;

            if (isMissingRefreshToken || tokenExpiredOrAboutToExpire)
            {
              logger.LogInformation("Refresh token needs rotation.");
              var tokenRefreshResult = playerIdentity.RotateRefreshToken(systemService,
                request.NewPlayerIdentityRequest.RefreshTokenByteCount,
                TimeSpan.FromMinutes(request.NewPlayerIdentityRequest.RefreshTokenAgeMinutes));

              if (!tokenRefreshResult.TryGetSuccessful(out _, out var refreshFailure))
              {
                logger.LogError(refreshFailure.GetException(), "Failed to renew refresh token.");
                return refreshFailure;
              }
            }

            await gameDb.SaveChangesAsync(cancellationToken);

            return new GetOrCreatePlayerResult(false,
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
