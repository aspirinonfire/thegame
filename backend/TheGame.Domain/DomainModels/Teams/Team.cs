using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Games.Events;

namespace TheGame.Domain.DomainModels.Teams
{
  public partial class Team : BaseModel
  {
    public const string InvalidPlayerError = "invalid_player";
    public const string ActiveGameAlreadyExistsError = "active_game_already_exists";
    public const string NoActiveGameError = "active_game_not_found";

    protected HashSet<Player> _players = new();
    protected HashSet<Game> _games = new();

    public IEnumerable<Game> Games => _games;
    public IEnumerable<Player> Player => _players;

    public long Id { get; }
    public string Name { get; protected set; }

    public virtual Result<Player> AddPlayer(Player player)
    {
      _players.Add(player);
      return Result.Success(player);
    }

    public virtual Result<Game> StartNewGame(IGameFactory gameFactory,
      string name,
      Player actingPlayer)
    {
      if (actingPlayer == null || !_players.Contains(actingPlayer))
      {
        return Result.Error<Game>(InvalidPlayerError);
      }

      if (_games.Any(game => game.IsActive))
      {
        return Result.Error<Game>(ActiveGameAlreadyExistsError);
      }

      var newGameResult = gameFactory.CreateNewGame(name);
      if (newGameResult.IsSuccess)
      {
        _games.Add(newGameResult.Value);
        AddDomainEvent(new NewGameStartedEvent());
      }

      return newGameResult;
    }

    public virtual Result<Game> FinishActiveGame(Player actingPlayer)
    {
      if (actingPlayer == null || !_players.Contains(actingPlayer))
      {
        return Result.Error<Game>(InvalidPlayerError);
      }

      var activeGame = _games.FirstOrDefault(game => game.IsActive);
      if (activeGame == null)
      {
        return Result.Error<Game>(NoActiveGameError);
      }

      var result = activeGame.FinishGame(System.DateTimeOffset.UtcNow);
      if (result.IsSuccess)
      {
        AddDomainEvent(new ExistingGameFinishedEvent());
      }
      return result;
    }
  }
}
