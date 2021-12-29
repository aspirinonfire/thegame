using System.Collections.Generic;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Tests.Domain.Players
{
  public class MockPlayer : Player
  {
    public MockPlayer(IEnumerable<Team>? teams, long userId, string name)
    {
      _teams = teams != null ?
        new HashSet<Team>(teams) :
        new ();

      UserId = userId;

      Name = name;
    }
  }
}
