using thegame.domain.DomainModels.Common;
using thegame.domain.DomainModels.Game;
using thegame.domain.DomainModels.Player;

namespace thegame.domain.DomainModels.Team
{
  public interface ITeamService
  {
    Result<TeamModel> CreateNewTeam(string name);
    Result<GameModel> AddNewGame(string name, PlayerModel player, TeamModel team);
  }
}
