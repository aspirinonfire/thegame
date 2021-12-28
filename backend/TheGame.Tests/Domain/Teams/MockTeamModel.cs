using System.Collections.Generic;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Tests.Domain.Teams
{
  public class MockTeamModel : Team
  {
    public MockTeamModel(IEnumerable<Player>? players,
      IEnumerable<Game>? games,
      string name)
    {
      _players = players != null ?
        new HashSet<Player>(players) :
        new ();

      _games = games != null ?
        new HashSet<Game>(games) :
        new ();

      Name = name;
    }
  }
}
