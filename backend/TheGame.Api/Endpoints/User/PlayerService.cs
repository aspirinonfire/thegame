using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.Utils;

namespace TheGame.Api.Endpoints.User;

public sealed record PlayerInfo(string PlayerName, long? PlayerId);

public sealed record GetOrCreatePlayerRequest(NewPlayerIdentityRequest NewPlayerIdentityRequest)
{
  public sealed record Result(bool IsNewIdentity,
    long PlayerIdentityId,
    long PlayerId,
    string ProviderName,
    string ProviderIdentityId,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiration);
}

public interface IPlayerService
{
  Task<Result<GetOrCreatePlayerRequest.Result>> GetOrCreatePlayer(GetOrCreatePlayerRequest request, CancellationToken cancellationToken);

  IQueryable<PlayerInfo> GetPlayerInfoQuery(long playerId);
}

public sealed class PlayerService(IGameDbContext gameDb,
  IPlayerIdentityFactory playerIdentityFactory,
  TimeProvider timeProvider,
  ILogger<GetOrCreatePlayerRequest> logger) : IPlayerService
{
  public IQueryable<PlayerInfo> GetPlayerInfoQuery(long playerId)
  {
    return gameDb
      .Players
      .AsNoTracking()
      .Where(player => player.Id == playerId)
      .Select(player => new PlayerInfo(player.Name, player.PlayerIdentityId));
  }

  public async Task<Result<GetOrCreatePlayerRequest.Result>> GetOrCreatePlayer(GetOrCreatePlayerRequest request, CancellationToken cancellationToken)
  {
    bool isNewIdentity;

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
      isNewIdentity = true;
      logger.LogInformation("New player with identity was created successfully.");
    }
    else
    {
      isNewIdentity = false;
      logger.LogInformation("Found an existing player with identity.");
    }

    var isMissingRefreshToken = string.IsNullOrWhiteSpace(playerIdentity.RefreshToken);
    var tokenExpiredOrAboutToExpire = playerIdentity.RefreshTokenExpiration.GetValueOrDefault().Add(TimeSpan.FromMinutes(5)) <= timeProvider.GetUtcNow();

    if (isMissingRefreshToken || tokenExpiredOrAboutToExpire)
    {
      logger.LogInformation("Refresh token needs rotation.");
      var tokenRefreshResult = playerIdentity.RotateRefreshToken(timeProvider,
        request.NewPlayerIdentityRequest.RefreshTokenByteCount,
        TimeSpan.FromMinutes(request.NewPlayerIdentityRequest.RefreshTokenAgeMinutes));

      if (!tokenRefreshResult.TryGetSuccessful(out _, out var refreshFailure))
      {
        logger.LogError(refreshFailure.GetException(), "Failed to renew refresh token.");
        return refreshFailure;
      }
    }

    await gameDb.SaveChangesAsync(cancellationToken);

    return new GetOrCreatePlayerRequest.Result(isNewIdentity,
      playerIdentity.Id,
      playerIdentity.Player!.Id,
      playerIdentity.ProviderName,
      playerIdentity.ProviderIdentityId,
      playerIdentity.RefreshToken!,
      playerIdentity.RefreshTokenExpiration.GetValueOrDefault());
  }
}
