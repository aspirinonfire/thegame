using System;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public interface IGamePlayerFactory
{
  OneOf<GamePlayer, Failure> AddPlayer(Player playerToAdd, Game currentGame);
}

public partial class GamePlayer
{
  public class GamePlayerFactory : IGamePlayerFactory
  {
    public const string PlayerAlreadyInvitedError = "player_already_invited";
    public const string InactiveGameInviteError = "player_invite_inactive_game";

    public OneOf<GamePlayer, Failure> AddPlayer(Player playerToAdd, Game currentGame)
    {
      if (!currentGame.IsActive)
      {
        return new Failure(InactiveGameInviteError);
      }

      if (currentGame.InvitedPlayers.Contains(playerToAdd))
      {
        return new Failure(PlayerAlreadyInvitedError);
      }

      var newPlayerInvite = new GamePlayer()
      {
        Game = currentGame,
        Player = playerToAdd,
        InvitationToken = Guid.NewGuid(),
        InviteStatus = GamePlayerInviteStatus.Created
      };

      currentGame.GamePlayerInvites.Add(newPlayerInvite);

      return newPlayerInvite;
    }
  }
}


