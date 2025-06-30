using System;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerActionsFactory
{
  IPlayerActions CreatePlayerActions(long actingPlayerId);
}

public sealed class PlayerActionsFactory(IGameDbContext gameDbContext,
  IGameScoreCalculator scoreCalculator,
  TimeProvider timeProvider,
  IGameLicensePlateFactory licensePlateSpotFactory)
  : IPlayerActionsFactory
{
  public IPlayerActions CreatePlayerActions(long actingPlayerId)
  {
    return new Player.PlayerActions(gameDbContext,
      scoreCalculator,
      timeProvider,
      licensePlateSpotFactory,
      actingPlayerId);
  }
}
