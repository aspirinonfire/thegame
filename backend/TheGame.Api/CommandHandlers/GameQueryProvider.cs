using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Api.CommandHandlers;

public sealed record OwnedOrInvitedGame
{
  public bool IsOwner { get; init; }
  public string CreatedByPlayerName { get; init; } = string.Empty;
  public long GameId { get; init; }
  public string GameName { get; init; } = string.Empty;
  public DateTimeOffset DateCreated { get; init; }
  public DateTimeOffset? DateModified { get; init; }
  public DateTimeOffset? EndedOn { get; init; }
  public IReadOnlyCollection<SpottedGamePlate> SpottedPlates { get; init; } = [];
  public GameScore GameScore { get; init; } = new GameScore(ReadOnlyCollection<string>.Empty, 0);

  public static OwnedOrInvitedGame FromGame(Game game, long playerId) => new()
  {
    IsOwner = game.CreatedBy.Id == playerId,
    CreatedByPlayerName = game.CreatedBy.Name,
    GameId = game.Id,
    GameName = game.Name,
    DateCreated = game.DateCreated,
    DateModified = game.DateModified,
    EndedOn = game.EndedOn,
    GameScore = game.GameScore,
    SpottedPlates = game.GameLicensePlates
      .Select(spot => new SpottedGamePlate
      {
        Country = spot.LicensePlate.Country,
        StateOrProvince = spot.LicensePlate.StateOrProvince,
        SpottedOn = spot.DateCreated,
        SpottedByPlayerId = spot.SpottedByPlayerId,
        SpottedByPlayerName = spot.SpottedBy.Name
      })
      .ToList()
  };
}

public sealed record SpottedGamePlate
{
  public Country Country { get; init; }
  
  public StateOrProvince StateOrProvince { get; init; }
  
  public long SpottedByPlayerId { get; init; }

  public string SpottedByPlayerName { get; init; } = string.Empty;
  
  public DateTimeOffset SpottedOn { get; init; }
}

public interface IGameQueryProvider
{
  Task<IReadOnlyCollection<OwnedOrInvitedGame>> GetOwnedAndInvitedGamesQuery(long playerId);
}

public class GameQueryProvider(IGameDbContext gameDbContext) : IGameQueryProvider
{
  // TODO this is inefficient, fix it with more targeted navigation properties or a more complex query
  public async Task<IReadOnlyCollection<OwnedOrInvitedGame>> GetOwnedAndInvitedGamesQuery(long playerId)
  {
    var toOwnedOrInvitedGameProjection = CreateGameToOwnedOrInvitedGameExpression(playerId);

    var ownedGames = await gameDbContext.Players.AsNoTracking()
      .Where(player => player.Id == playerId)
      .SelectMany(player => player.OwnedGames)
      .Select(toOwnedOrInvitedGameProjection)
      .ToListAsync();

    var invitedGames = await gameDbContext.Players.AsNoTracking()
      .Where(player => player.Id == playerId)
      .SelectMany(player => player.InvatedGamePlayers)
      .Select(gamePlayer => gamePlayer.Game)
      .Select(toOwnedOrInvitedGameProjection)
      .ToListAsync();

    return ownedGames
      .Concat(invitedGames)
      .OrderByDescending(game => game.DateCreated)
      .ToArray();
  }

  private static Expression<Func<Game, OwnedOrInvitedGame>> CreateGameToOwnedOrInvitedGameExpression(long playerId) =>
    game => new OwnedOrInvitedGame
    {
      IsOwner = game.CreatedBy.Id == playerId,
      CreatedByPlayerName = game.CreatedBy.Name,
      GameId = game.Id,
      GameName = game.Name,
      DateCreated = game.DateCreated,
      DateModified = game.DateModified,
      EndedOn = game.EndedOn,
      GameScore = game.GameScore,
      SpottedPlates = game.GameLicensePlates
        .Select(spot => new SpottedGamePlate
        {
          Country = spot.LicensePlate.Country,
          StateOrProvince = spot.LicensePlate.StateOrProvince,
          SpottedOn = spot.DateCreated,
          SpottedByPlayerId = spot.SpottedByPlayerId,
          SpottedByPlayerName = spot.SpottedBy.Name
        })
        .ToList()
    };
}
