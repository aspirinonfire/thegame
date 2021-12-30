using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.Utils;

namespace TheGame.Domain.DomainModels.Teams
{
  public partial class Team : BaseModel
  {
    public const string PlayerAlreadyExistError = "player_already_exist";
    public const string InvalidPlayerError = "invalid_player";
    public const string ActiveGameAlreadyExistsError = "active_game_already_exists";
    public const string NoActiveGameError = "active_game_not_found";

    protected HashSet<Player> _players = new();
    protected HashSet<Game> _games = new();

    public virtual ICollection<Game> Games => _games;
    public virtual ICollection<Player> Players => _players;

    public long Id { get; }
    public string Name { get; protected set; }

    public virtual Result<Player> AddNewPlayer(long userId, string playerName)
    {
      if (Players.Any(player => player.UserId == userId))
      {
        return Result.Error<Player>(PlayerAlreadyExistError);
      }

      var player = new Player(userId, playerName);

      GetWriteableCollection(Players).Add(player);
      return Result.Success(player);
    }

    public virtual Result<Player> AddExistingPlayer(Player player)
    {
      if (Players.Contains(player))
      {
        return Result.Error<Player>(PlayerAlreadyExistError);
      }

      GetWriteableCollection(Players).Add(player);
      return Result.Success(player);
    }

    public virtual Result<Game> StartNewGame(IGameFactory gameFactory,
      string name,
      Player actingPlayer)
    {
      if (actingPlayer == null || !Players.Contains(actingPlayer))
      {
        return Result.Error<Game>(InvalidPlayerError);
      }

      if (Games.Any(game => game.IsActive))
      {
        return Result.Error<Game>(ActiveGameAlreadyExistsError);
      }

      var newGameResult = gameFactory.CreateNewGame(name);
      if (newGameResult.IsSuccess)
      {
        GetWriteableCollection(Games).Add(newGameResult.Value);
        AddDomainEvent(new NewGameStartedEvent());
      }

      return newGameResult;
    }

    public virtual Result<Game> FinishActiveGame(ISystemService systemService, Player actingPlayer)
    {
      if (actingPlayer == null || !Players.Contains(actingPlayer))
      {
        return Result.Error<Game>(InvalidPlayerError);
      }

      var activeGame = Games.FirstOrDefault(game => game.IsActive);
      if (activeGame == null)
      {
        return Result.Error<Game>(NoActiveGameError);
      }

      var result = activeGame.FinishGame(systemService.DateTimeOffset.UtcNow);
      if (result.IsSuccess)
      {
        AddDomainEvent(new ExistingGameFinishedEvent());
      }
      return result;
    }
  }
}
