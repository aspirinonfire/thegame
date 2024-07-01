using FluentValidation;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.Players;

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

  protected HashSet<Player> _players = [];
  protected HashSet<Game> _games = [];

  public virtual ICollection<Game> Games => _games;
  public virtual ICollection<Player> Players => _players;

  public long Id { get; }
  public string Name { get; protected set; } = default!;

  public Team() { }

  public virtual OneOf<Player, Failure> AddNewPlayer(IPlayerFactory playerFactory, long userId, string playerName)
  {
    var inlineValidator = new InlineValidator<Team>();

    inlineValidator
      .RuleForEach(team => team.Players)
        .Must(player => player.UserId != userId)
        .WithMessage(ErrorMessages.PlayerAlreadyExistError);

    var validationResult = inlineValidator.Validate(this);

    if (!validationResult.IsValid)
    {
      return Failure.CreateFromValidationErrors(validationResult.Errors);
    }

    var playerResult = playerFactory.CreateNewPlayer(userId, playerName);
    if (!playerResult.TryGetSuccessful(out var newPlayer, out var playerFailure))
    {
      return playerFailure;
    }

    GetWriteableCollection(Players).Add(newPlayer);
    return newPlayer;
  }

  public virtual OneOf<Player, Failure> AddExistingPlayer(Player player)
  {
    var inlineValidator = new InlineValidator<Team>();
    inlineValidator
      .RuleForEach(team => team.Players)
      .Must(teamPlayer => teamPlayer != player)
      .WithMessage(ErrorMessages.PlayerAlreadyExistError);

    var validationResult = inlineValidator.Validate(this);
    if (!validationResult.IsValid)
    {
      return Failure.CreateFromValidationErrors(validationResult.Errors);
    }

    GetWriteableCollection(Players).Add(player);
    return player;
  }

  public virtual OneOf<Game, Failure> StartNewGame(IGameFactory gameFactory,
    string name,
    Player actingPlayer)
  {
    var inlineValidator = new InlineValidator<Team>();
    inlineValidator
      .RuleForEach(team => team.Games)
        .Must(game => !game.IsActive)
        .WithMessage(ErrorMessages.ActiveGameAlreadyExistsError);

    var validationResult = inlineValidator.Validate(this);
    if (!validationResult.IsValid)
    {
      return Failure.CreateFromValidationErrors(validationResult.Errors);
    }

    var newGameResult = gameFactory.CreateNewGame(name);
    if (newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
    {
      GetWriteableCollection(Games).Add(newGame);
      AddDomainEvent(new NewGameStartedEvent());
    }
    else
    {
      return newGameFailure;
    }

    return newGameResult;
  }

  public virtual OneOf<Game, Failure> FinishActiveGame(ISystemService systemService, Player actingPlayer)
  {
    if (!Players.Contains(actingPlayer))
    {
      return new Failure(ErrorMessages.InvalidPlayerError);
    }

    var activeGame = Games.FirstOrDefault(game => game.IsActive);
    if (activeGame == null)
    {
      return new Failure(ErrorMessages.NoActiveGameError);
    }

    var finishGameResult = activeGame.FinishGame(systemService.DateTimeOffset.UtcNow);
    if (finishGameResult.TryGetSuccessful(out var successfulResult, out var finishGameFailure))
    {
      AddDomainEvent(new ExistingGameFinishedEvent());
      return successfulResult;
    }
    else
    {
      return finishGameFailure;
    }
  }
}
