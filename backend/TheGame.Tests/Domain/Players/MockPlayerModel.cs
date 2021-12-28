using System.Collections.Generic;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Tests.Domain.Players
{
  public class MockPlayerModel : Player
  {
    public MockPlayerModel(IEnumerable<Team>? teams, long userId, string name)
    {
      _teams = teams != null ?
        new HashSet<Team>(teams) :
        new ();

      UserId = userId;

      Name = name;
    }
  }
}
