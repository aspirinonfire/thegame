using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Teams
{
  public interface ITeamService
  {
    DomainResult<Team> CreateNewTeam(string name);
  }
}
