using Microsoft.EntityFrameworkCore;
using System.Linq;
using TheGame.Domain.DomainModels;

namespace TheGame.Api.CommandHandlers;

public sealed record PlayerInfo(string PlayerName, long? PlayerId);

public interface IPlayerQueryProvider
{
  IQueryable<PlayerInfo> GetPlayerInfoQuery(long playerId);
}

public class PlayerQueryProvider(IGameDbContext gameDbContext) : IPlayerQueryProvider
{
  public IQueryable<PlayerInfo> GetPlayerInfoQuery(long playerId)
  {
    return gameDbContext
      .Players
      .AsNoTracking()
      .Where(player => player.Id == playerId)
      .Select(player => new PlayerInfo(player.Name, player.PlayerIdentityId));
  }
}
