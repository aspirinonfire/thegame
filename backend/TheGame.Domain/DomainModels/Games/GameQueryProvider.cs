using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;

namespace TheGame.Domain.DomainModels.Games
{
  public sealed record OwnedAndInvitedGames
  {
    public bool IsOwner { get; init; }
    public long GameId { get; init; }
    public string GameName { get; init; } = string.Empty;
    public DateTimeOffset DateCreated { get; init; }
    public DateTimeOffset? DateModified { get; init; }
    public DateTimeOffset? EndedOn { get; init; }
    public int NumberOfSpottedPlates { get; init; }
  }

  public interface IGameQueryProvider
  {
    IQueryable<OwnedAndInvitedGames> GetOwnedAndInvitedGamesQuery(long playerId);
  }

  public class GameQueryProvider(IGameDbContext gameDbContext) : IGameQueryProvider
  {
    public IQueryable<OwnedAndInvitedGames> GetOwnedAndInvitedGamesQuery(long playerId)
    {
      var ownedGames = gameDbContext
        .Games
        .AsNoTracking()
        .Where(game => game.CreatedBy.Id == playerId)
        .Select(game => new OwnedAndInvitedGames
        {
          IsOwner = true,
          GameId = game.Id,
          GameName = game.Name,
          DateCreated = game.DateCreated,
          DateModified = game.DateModified,
          EndedOn = game.EndedOn,
          NumberOfSpottedPlates = game.GameLicensePlates.Count
        });

      var invitedGames = gameDbContext
        .Games
        .AsNoTracking()
        .SelectMany(game => game.GamePlayerInvites)
        .Where(gameInvite => gameInvite.Player.Id == playerId)
        .Select(gameInvite => new OwnedAndInvitedGames
        {
          IsOwner = false,
          GameId = gameInvite.Game.Id,
          GameName = gameInvite.Game.Name,
          DateCreated = gameInvite.Game.DateCreated,
          DateModified = gameInvite.Game.DateModified,
          EndedOn = gameInvite.Game.EndedOn,
          NumberOfSpottedPlates = gameInvite.Game.GameLicensePlates.Count()
        });

      return ownedGames.Concat(invitedGames).OrderByDescending(game => game.DateCreated);
    }
  }
}
