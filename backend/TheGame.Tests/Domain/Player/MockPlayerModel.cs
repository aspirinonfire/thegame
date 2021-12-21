using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Player;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Tests.Domain.Player
{
  internal class MockPlayerModel : PlayerModel
  {
    public MockPlayerModel(IEnumerable<TeamModel>? teams, long userId, string name)
    {
      _teams = teams == null ? new HashSet<TeamModel>() : new HashSet<TeamModel>(teams);
      UserId = userId;
      Name = name;
    }
  }
}
