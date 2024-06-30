using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.Utils;

namespace TheGame.Domain.DomainModels.Teams;

public partial class Team : BaseModel
{
  public static class ErrorMessages
  {
    public const string PlayerAlreadyExistError = "player_already_exist";
    public const string InvalidPlayerError = "invalid_player";
    public const string ActiveGameAlreadyExistsError = "active_game_already_exists";
    public const string NoActiveGameError = "active_game_not_found";
  }

  protected HashSet<Player> _players = new();
  protected HashSet<Game> _games = new();

  public virtual ICollection<Game> Games => _games;
  public virtual ICollection<Player> Players => _players;

  public long Id { get; }
  public string Name { get; protected set; }

  public Team()
  {
    // Autopopulated by EF
    Name = null!;
  }

  public virtual DomainResult<Player> AddNewPlayer(IPlayerFactory playerFactory, long userId, string playerName)
  {
    if (Players.Any(player => player.UserId == userId))
    {
      return DomainResult.Error<Player>(ErrorMessages.PlayerAlreadyExistError);
    }

    var playerResult = playerFactory.CreateNewPlayer(userId, playerName);
    if (!playerResult.IsSuccess || playerResult.HasNoValue)
    {
      return DomainResult.Error<Player>(ErrorMessages.InvalidPlayerError);
    }

    GetWriteableCollection(Players).Add(playerResult.Value!);
    return DomainResult.Success(playerResult.Value!);
  }

  public virtual DomainResult<Player> AddExistingPlayer(Player player)
  {
    if (Players.Contains(player))
    {
      return DomainResult.Error<Player>(ErrorMessages.PlayerAlreadyExistError);
    }

    GetWriteableCollection(Players).Add(player);
    return DomainResult.Success(player);
  }

  public virtual DomainResult<Game> StartNewGame(IGameFactory gameFactory,
    string name,
    Player actingPlayer)
  {
    if (!Players.Contains(actingPlayer))
    {
      return DomainResult.Error<Game>(ErrorMessages.InvalidPlayerError);
    }

    if (Games.Any(game => game.IsActive))
    {
      return DomainResult.Error<Game>(ErrorMessages.ActiveGameAlreadyExistsError);
    }

    var newGameResult = gameFactory.CreateNewGame(name);
    if (newGameResult.IsSuccess && !newGameResult.HasNoValue)
    {
      GetWriteableCollection(Games).Add(newGameResult.Value!);
      AddDomainEvent(new NewGameStartedEvent());
    }

    return newGameResult;
  }

  public virtual DomainResult<Game> FinishActiveGame(ISystemService systemService, Player actingPlayer)
  {
    if (!Players.Contains(actingPlayer))
    {
      return DomainResult.Error<Game>(ErrorMessages.InvalidPlayerError);
    }

    var activeGame = Games.FirstOrDefault(game => game.IsActive);
    if (activeGame == null)
    {
      return DomainResult.Error<Game>(ErrorMessages.NoActiveGameError);
    }

    var result = activeGame.FinishGame(systemService.DateTimeOffset.UtcNow);
    if (result.IsSuccess)
    {
      AddDomainEvent(new ExistingGameFinishedEvent());
    }
    return result;
  }
}
