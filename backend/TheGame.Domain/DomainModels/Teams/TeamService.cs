using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Teams
{
  public partial class Team
  {
    public class TeamService : ITeamService
    {
      public Result<Team> CreateNewTeam(string name)
      {
        var newTeam = new Team
        {
          Name = name
        };
        // TODO add to tracking

        return Result.Success(newTeam);
      }
    }
  }
}
