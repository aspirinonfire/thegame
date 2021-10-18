using thegame.domain.DomainModels.Common;

namespace thegame.domain.DomainModels.Services
{
    public interface ITeamService
    {
        Result<Team> CreateNewTeam(string name);
        Result<Game> AddNewGame(string name, Player player, Team team);
    }
}