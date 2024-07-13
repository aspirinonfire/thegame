using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Games;

public sealed record OwnedAndInvitedGames
{
  public bool IsOwner { get; init; }
  public long GameId { get; init; }
  public string GameName { get; init; } = string.Empty;
  public DateTimeOffset DateCreated { get; init; }
  public DateTimeOffset? DateModified { get; init; }
  public DateTimeOffset? EndedOn { get; init; }
  public ICollection<SpottedGamePlate> SpottedPlates { get; init; } = [];
  public GameScore GameScore { get; init; } = new GameScore(ReadOnlyCollection<string>.Empty, 0);
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
  IQueryable<OwnedAndInvitedGames> GetOwnedAndInvitedGamesQuery(long playerId);
}

public class GameQueryProvider(IGameDbContext gameDbContext) : IGameQueryProvider
{
  public IQueryable<OwnedAndInvitedGames> GetOwnedAndInvitedGamesQuery(long playerId)
  {
    IQueryable<OwnedAndInvitedGames>? ownedAndInvitedGames = gameDbContext
      .Games
      .AsNoTracking()
      .Where(game => game.CreatedBy.Id == playerId ||
        game.GamePlayerInvites.Any(invite => invite.Player.Id == playerId))
      .OrderByDescending(game => game.DateCreated)
      .Select(game => new OwnedAndInvitedGames
      {
        IsOwner = game.CreatedBy.Id == playerId,
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
      });

    return ownedAndInvitedGames;
  }
}
