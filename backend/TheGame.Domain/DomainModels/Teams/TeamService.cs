using TheGame.Domain.DAL;

namespace TheGame.Domain.DomainModels.Teams;

public interface ITeamService
{
  OneOf<Team, Failure> CreateNewTeam(string name);
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

    public OneOf<Team, Failure> CreateNewTeam(string name)
    {
      if (string.IsNullOrEmpty(name))
      {
        return new Failure(InvalidTeamNameError);
      }

      var newTeam = new Team
      {
        Name = name
      };
      _dbContext.Teams.Add(newTeam);

      return newTeam;
    }
  }
}
