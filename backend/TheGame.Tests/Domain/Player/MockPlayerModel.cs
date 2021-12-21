using System.Collections.Generic;
using TheGame.Domain.DomainModels.Player;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Tests.Domain.Player
{
  public class MockPlayerModel : PlayerModel
  {
    public MockPlayerModel(IEnumerable<TeamModel>? teams, long userId, string name)
    {
      _teams = teams != null ?
        new HashSet<TeamModel>(teams) :
        new ();

      UserId = userId;

      Name = name;
    }
  }
}
