using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Team
{
  public partial class TeamModel
  {
    public class TeamService : ITeamService
    {
      public Result<TeamModel> CreateNewTeam(string name)
      {
        var newTeam = new TeamModel
        {
          Name = name
        };
        // TODO add to tracking

        return Result.Success(newTeam);
      }
    }
  }
}
