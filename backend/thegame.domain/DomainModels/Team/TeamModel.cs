using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.Player;

namespace TheGame.Domain.DomainModels.Team
{
  public partial class TeamModel : BaseModel
  {
    public const string InvalidPlayerError = "invalid_player";
    public const string GameAlreadyExistsError = "active_game_already_exists";
    public const string NoActiveGameError = "active_game_not_found";

    protected HashSet<PlayerModel> _players = new();
    protected HashSet<GameModel> _games = new();

    // TODO add navigation props!

    public long Id { get; protected set; }
    public string Name { get; protected set; }

    public Result<PlayerModel> AddPlayer(PlayerModel player)
    {
      _players.Add(player);
      return Result.Success(player);
    }

    public Result<GameModel> AddNewGame(IGameFactory gameFactory,
      string name,
      PlayerModel actingPlayer)
    {
      if (actingPlayer == null || !_players.Contains(actingPlayer))
      {
        return Result.Error<GameModel>(InvalidPlayerError);
      }

      if (_games.Any(game => game.IsActive))
      {
        return Result.Error<GameModel>(GameAlreadyExistsError);
      }

      var newGameResult = gameFactory.CreateNewGame(name);
      if (newGameResult.IsSuccess)
      {
        _games.Add(newGameResult.Value);
      }

      return newGameResult;
    }

    public Result<GameModel> FinishActiveGame(PlayerModel actingPlayer)
    {
      if (actingPlayer == null || !_players.Contains(actingPlayer))
      {
        return Result.Error<GameModel>(InvalidPlayerError);
      }

      var activeGame = _games.FirstOrDefault(game => game.IsActive);
      if (activeGame == null)
      {
        return Result.Error<GameModel>(NoActiveGameError);
      }

      return activeGame.EndGame(System.DateTimeOffset.UtcNow);
    }
  }
}
