using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Teams;

public interface ITeamService
{
  DomainResult<Team> CreateNewTeam(string name);
}

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
