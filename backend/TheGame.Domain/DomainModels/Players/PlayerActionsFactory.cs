using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerActionsFactory
{
  IAsyncEnumerable<IPlayerActions> GetPlayersWithActions(IQueryable<Player> playerQuery);

  IPlayerFactory CreatePlayerFactory();
}

public sealed class PlayerActionsFactory(IGameDbContext gameDbContext,
  IGameScoreCalculator scoreCalculator,
  TimeProvider timeProvider,
  IGameLicensePlateFactory licensePlateSpotFactory)
  : IPlayerActionsFactory
{
  public IPlayerFactory CreatePlayerFactory() => new Player.PlayerFactory(gameDbContext);

  public IAsyncEnumerable<IPlayerActions> GetPlayersWithActions(IQueryable<Player> playerQuery)
  {
    var playerFactory = CreatePlayerFactory();

    return playerQuery
      .Include(p => p.InvitedGames)
        .ThenInclude(ig => ig.GameLicensePlates)
          .ThenInclude(glp => glp.LicensePlate)
      .Include(p => p.OwnedGames)
        .ThenInclude(ig => ig.GameLicensePlates)
          .ThenInclude(glp => glp.LicensePlate)
      .Include(p => p.OwnedGames)
        .ThenInclude(g => g.InvitedPlayers)
      .Include(p => p.InvatedGamePlayers)
      .AsAsyncEnumerable()
      .Select(player =>
        new Player.PlayerActions(gameDbContext,
          scoreCalculator,
          playerFactory,
          timeProvider,
          licensePlateSpotFactory,
          player));
  }
}
