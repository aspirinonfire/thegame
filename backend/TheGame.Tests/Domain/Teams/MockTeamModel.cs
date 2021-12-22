using System.Collections.Generic;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Tests.Domain.Teams
{
  public class MockTeamModel : TeamModel
  {
    public MockTeamModel(IEnumerable<PlayerModel>? players,
      IEnumerable<GameModel>? games,
      string name)
    {
      _players = players != null ?
        new HashSet<PlayerModel>(players) :
        new ();

      _games = games != null ?
        new HashSet<GameModel>(games) :
        new ();

      Name = name;
    }
  }
}
