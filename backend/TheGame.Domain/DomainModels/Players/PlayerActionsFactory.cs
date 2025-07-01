using System;
using System.Linq;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerActionsFactory
{
  IPlayerActions GetPlayerActions(IQueryable<Player> playerQuery);

  IPlayerFactory CreatePlayerFactory();
}

public sealed class PlayerActionsFactory(IGameDbContext gameDbContext,
  IGameScoreCalculator scoreCalculator,
  TimeProvider timeProvider,
  IGameLicensePlateFactory licensePlateSpotFactory)
  : IPlayerActionsFactory
{
  public IPlayerFactory CreatePlayerFactory() => new Player.PlayerFactory(gameDbContext);

  public IPlayerActions GetPlayerActions(IQueryable<Player> playerQuery)
  {
    var playerFactory = CreatePlayerFactory();

    return new Player.PlayerActions(gameDbContext,
      scoreCalculator,
      playerFactory,
      timeProvider,
      licensePlateSpotFactory,
      playerQuery);
  }
}
