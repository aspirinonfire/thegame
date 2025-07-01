using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Tests.DomainModels.Players;

public class MockPlayer : Player
{
  public MockPlayer() { }

  public MockPlayer(long playerId, string name)
  {
    Id = playerId;
    Name = name;
    _ownedGames = [];
    InvitedGames = new HashSet<Game>(0);
  }

  public void SetOwnedGames(HashSet<Game> newOwnedGames)
  {
    _ownedGames = newOwnedGames;
  }

  public void SetInvitedGames(HashSet<Game> newInvitedGames)
  {
    InvitedGames = newInvitedGames;
  }
}
