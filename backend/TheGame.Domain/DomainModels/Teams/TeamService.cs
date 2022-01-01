using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Teams
{
  public partial class Team
  {
    public class TeamService : ITeamService
    {
      public const string InvalidTeamNameError = "invalid_team_name";
      private readonly IGameDbContext _dbContext;

      public TeamService(IGameDbContext dbContext)
      {
        _dbContext = dbContext;
      }

      public DomainResult<Team> CreateNewTeam(string name)
      {
        if (string.IsNullOrEmpty(name))
        {
          return DomainResult.Error<Team>(InvalidTeamNameError);
        }

        var newTeam = new Team
        {
          Name = name
        };
        _dbContext.Teams.Add(newTeam);

        return DomainResult.Success(newTeam);
      }
    }
  }
}
