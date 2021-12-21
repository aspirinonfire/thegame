using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.Player;
using TheGame.Domain.DomainModels.Team.Events;

namespace TheGame.Domain.DomainModels.Team
{
  public partial class TeamModel : BaseModel
  {
    public const string InvalidPlayerError = "invalid_player";
    public const string ActiveGameAlreadyExistsError = "active_game_already_exists";
    public const string NoActiveGameError = "active_game_not_found";

    protected HashSet<PlayerModel> _players = new();
    protected HashSet<GameModel> _games = new();

    public IEnumerable<GameModel> Games => _games;
    public IEnumerable<PlayerModel> Player => _players;

    public long Id { get; }
    public string Name { get; protected set; }

    public virtual Result<PlayerModel> AddPlayer(PlayerModel player)
    {
      _players.Add(player);
      return Result.Success(player);
    }

    public virtual Result<GameModel> StartNewGame(IGameFactory gameFactory,
      string name,
      PlayerModel actingPlayer)
    {
      if (actingPlayer == null || !_players.Contains(actingPlayer))
      {
        return Result.Error<GameModel>(InvalidPlayerError);
      }

      if (_games.Any(game => game.IsActive))
      {
        return Result.Error<GameModel>(ActiveGameAlreadyExistsError);
      }

      var newGameResult = gameFactory.CreateNewGame(name);
      if (newGameResult.IsSuccess)
      {
        _games.Add(newGameResult.Value);
        AddEvent(new NewGameStartedEvent());
      }

      return newGameResult;
    }

    public virtual Result<GameModel> FinishActiveGame(PlayerModel actingPlayer)
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

      var result = activeGame.FinishGame(System.DateTimeOffset.UtcNow);
      if (result.IsSuccess)
      {
        AddEvent(new ExistingGameFinishedEvent());
      }
      return result;
    }
  }
}
