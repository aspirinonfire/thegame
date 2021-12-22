using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Team
{
  public interface ITeamService
  {
    Result<TeamModel> CreateNewTeam(string name);
  }
}
