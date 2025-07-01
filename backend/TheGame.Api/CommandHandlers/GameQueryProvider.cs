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
  public bool IsOwner { get; internal set; }
  public long CreatedByPlayerId { get; init; }
  public string CreatedByPlayerName { get; init; } = string.Empty;
  public long GameId { get; init; }
  public string GameName { get; init; } = string.Empty;
  public DateTimeOffset DateCreated { get; init; }
  public DateTimeOffset? DateModified { get; init; }
  public DateTimeOffset? EndedOn { get; init; }
  public IReadOnlyCollection<SpottedGamePlate> SpottedPlates { get; init; } = [];
  public GameScore GameScore { get; init; } = new GameScore(ReadOnlyCollection<string>.Empty, 0);

  public static OwnedOrInvitedGame FromGame(Game game, long playerId)
  {
    var ownedOrInvitedGame = _fromGameCompiledExpression(game);
    ownedOrInvitedGame.IsOwner = game.CreatedByPlayerId == playerId;
    return ownedOrInvitedGame;
  }

  public static Expression<Func<Game, OwnedOrInvitedGame>> CreateGameToOwnedOrInvitedGameExpression() =>
    game => new OwnedOrInvitedGame
    {
      CreatedByPlayerId = game.CreatedBy.Id,
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

  private readonly static Func<Game, OwnedOrInvitedGame> _fromGameCompiledExpression = CreateGameToOwnedOrInvitedGameExpression().Compile();
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
  public async Task<IReadOnlyCollection<OwnedOrInvitedGame>> GetOwnedAndInvitedGamesQuery(long playerId)
  {
    // Dev notes:
    // 1. We must keep owned and invited games separate until after the materialization (see https://github.com/dotnet/efcore/issues/29718)
    // 2. We are using expressions to project to ensure EF can translate required navigations into SQL correctly.
    // 3. We use AsQueryable() to ensure that the projection is using correct .Select extension method.

    var gamesQuery = gameDbContext.Players
      .AsNoTracking()
      .Where(player => player.Id == playerId)
      .Select(player => new
      {
        ownedGames = player.OwnedGames
          .AsQueryable()
          .Select(OwnedOrInvitedGame.CreateGameToOwnedOrInvitedGameExpression())
          .ToArray(),

        invitedGames = player.GamePlayers
          .Select(gp => gp.Game)
          .AsQueryable()
          .Select(OwnedOrInvitedGame.CreateGameToOwnedOrInvitedGameExpression())
          .ToArray()
      });

    return (await gamesQuery.ToArrayAsync())
      .SelectMany(x => x.ownedGames.Concat(x.invitedGames))
      .Select(game =>
      {
        game.IsOwner = game.CreatedByPlayerId == playerId;
        return game;
      })
      .OrderByDescending(game => game.DateCreated)
      .ToArray();
  }
}
