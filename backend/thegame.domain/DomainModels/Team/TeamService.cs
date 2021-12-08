using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using thegame.domain.DomainModels.Common;
using thegame.domain.DomainModels.Game;
using thegame.domain.DomainModels.Player;

namespace thegame.domain.DomainModels.Team
{
  public partial class TeamModel
  {
    public class TeamService : ITeamService
    {
      private readonly IGameFactory _gameFactory;

      public TeamService(IGameFactory gameFactory)
      {
        this._gameFactory = gameFactory;
      }

      public Result<TeamModel> CreateNewTeam(string name)
      {
        var newTeam = new TeamModel
        {
          Name = name
        };
        // TODO add to tracking

        return Result.Success(newTeam);
      }

      public Result<GameModel> AddNewGame(string name, PlayerModel player, TeamModel team)
      {
        if (team == null || player == null)
        {
          return Result.Error<GameModel>("internal_error");
        }

        if (!team._players.Contains(player))
        {
          return Result.Error<GameModel>("invalid_player");
        }

        if (team._games.Any(game => game.IsActive))
        {
          return Result.Error<GameModel>("active_game_already_exists");
        }

        var newGameResult = _gameFactory.CreateNewGame(name);
        team._games.Add(newGameResult.Value);

        return Result.Success(newGameResult.Value);
      }
    }
  }
}
