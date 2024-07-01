using System.Collections.Generic;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Tests.Domain.Teams
{
  public class MockTeam : Team
  {
    public MockTeam(IEnumerable<Player>? players,
      IEnumerable<Game>? games,
      string name)
    {
      _players = players != null ? new HashSet<Player>(players) : [];

      _games = games != null ? new HashSet<Game>(games) : [];

      Name = name;
    }
  }
}
