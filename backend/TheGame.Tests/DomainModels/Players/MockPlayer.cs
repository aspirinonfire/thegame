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
  }

  public void SetOwnedGames(HashSet<Game> newOwnedGames)
  {
    _ownedGames = newOwnedGames;
  }
}
