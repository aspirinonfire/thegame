using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Teams;

public interface ITeamService
{
  OneOf<Player, Failure> AddExistingPlayer(Team team, Player player);
  OneOf<Player, Failure> AddNewPlayer(Team team, long userId, string playerName);
  Task<OneOf<Team, Failure>> CreateNewTeam(string name);
  OneOf<Game, Failure> FinishActiveGame(Team team, Player actingPlayer);
  OneOf<Game, Failure> StartNewGame(Team team, string newGameName, Player actingPlayer);
}

public partial class Team
{
  public class TeamService(IGameDbContext dbContext, IPlayerFactory playerFactory, IGameFactory gameFactory, ISystemService systemService) : ITeamService
  {
    public const string InvalidTeamNameError = "invalid_team_name";
    public const string DuplicateTeamNameError = "duplicate_team_name";
    public const string PlayerAlreadyExistError = "player_already_exist";
    public const string InvalidPlayerError = "invalid_player";
    public const string ActiveGameAlreadyExistsError = "active_game_already_exists";
    public const string NoActiveGameError = "active_game_not_found";

    public async Task<OneOf<Team, Failure>> CreateNewTeam(string name)
    {
      if (string.IsNullOrEmpty(name))
      {
        return new Failure(InvalidTeamNameError);
      }

      var duplicateName = await dbContext.Teams.AnyAsync(t => t.Name == name);
      if (duplicateName)
      {
        return new Failure(DuplicateTeamNameError);
      }

      var newTeam = new Team
      {
        Name = name
      };
      dbContext.Teams.Add(newTeam);

      return newTeam;
    }

    public OneOf<Player, Failure> AddNewPlayer(Team team, long userId, string playerName)
    {
      if (team.Players.Any(player => player.UserId == userId))
      {
        return new Failure(PlayerAlreadyExistError);
      }

      var playerResult = playerFactory.CreateNewPlayer(userId, playerName);
      if (!playerResult.TryGetSuccessful(out var newPlayer, out var playerFailure))
      {
        return playerFailure;
      }

      GetWriteableCollection(team.Players).Add(newPlayer);
      return newPlayer;
    }

    public OneOf<Player, Failure> AddExistingPlayer(Team team, Player player)
    {
      if (team.Players.Contains(player))
      {
        return new Failure(PlayerAlreadyExistError);
      }

      GetWriteableCollection(team.Players).Add(player);
      return player;
    }

    public OneOf<Game, Failure> StartNewGame(Team team,
      string newGameName,
      Player actingPlayer)
    {
      if (!team.Players.Contains(actingPlayer))
      {
        return new Failure(InvalidPlayerError);
      }

      if (team.Games.Any(game => game.IsActive))
      {
        return new Failure(ActiveGameAlreadyExistsError);
      }

      var newGameResult = gameFactory.CreateNewGame(newGameName);
      if (newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
      {
        GetWriteableCollection(team.Games).Add(newGame);
        team.AddDomainEvent(new NewGameStartedEvent());
      }
      else
      {
        return newGameFailure;
      }

      return newGameResult;
    }

    public OneOf<Game, Failure> FinishActiveGame(Team team, Player actingPlayer)
    {
      if (!team.Players.Contains(actingPlayer))
      {
        return new Failure(InvalidPlayerError);
      }

      var activeGame = team.Games.FirstOrDefault(game => game.IsActive);
      if (activeGame == null)
      {
        return new Failure(NoActiveGameError);
      }

      var finishGameResult = activeGame.FinishGame(systemService.DateTimeOffset.UtcNow);
      if (finishGameResult.TryGetSuccessful(out var successfulResult, out var finishGameFailure))
      {
        team.AddDomainEvent(new ExistingGameFinishedEvent());
        return successfulResult;
      }
      else
      {
        return finishGameFailure;
      }
    }
  }
}
