using System.Collections.Generic;
using System.Linq;
using thegame.domain.DomainModels.Common;
using thegame.domain.DomainModels.Services;

namespace thegame.domain.DomainModels
{
    public class Team : BaseModel
    {
        public class TeamService
        {
            private readonly IGameFactory _gameFactory;

            public TeamService(IGameFactory gameFactory)
            {
                this._gameFactory = gameFactory;
            }

            public Result<Game> AddNewGame(string name, Player player, Team team)
            {
                if (team == null || player == null)
                {
                    return Result.Error<Game>("internal_error");
                }

                if (!team._players.Contains(player))
                {
                    return Result.Error<Game>("invalid_player");
                }

                if (team._games.Any(game => game.IsActive))
                {
                    return Result.Error<Game>("active_game_already_exists");
                }

                var newGameResult = _gameFactory.CreateNewGame(name);
                team._games.Add(newGameResult.Value);

                return Result.Success(newGameResult.Value);
            }
        }

        protected HashSet<Player> _players;
        protected HashSet<Game> _games;

        public long Id { get; }
        public string Name { get; }
    }
}