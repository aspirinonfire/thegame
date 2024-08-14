using System;
using TheGame.Domain.DomainModels.Common;
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
    public OneOf<GamePlayer, Failure> AddPlayer(Player playerToAdd, Game currentGame)
    {
      if (!currentGame.IsActive)
      {
        return new Failure(ErrorMessageProvider.InactiveGameInviteError);
      }

      if (currentGame.InvitedPlayers.Contains(playerToAdd))
      {
        return new Failure(ErrorMessageProvider.PlayerAlreadyInvitedError);
      }

      var newPlayerInvite = new GamePlayer()
      {
        Player = playerToAdd,
        InvitationToken = Guid.NewGuid(),
        InviteStatus = GamePlayerInviteStatus.Created
      };

      return newPlayerInvite;
    }
  }
}


