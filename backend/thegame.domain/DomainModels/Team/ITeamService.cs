using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.Player;

namespace TheGame.Domain.DomainModels.Team
{
  public interface ITeamService
  {
    Result<TeamModel> CreateNewTeam(string name);
  }
}
