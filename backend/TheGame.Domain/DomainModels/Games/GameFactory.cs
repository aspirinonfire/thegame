using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public interface IGameFactory
{
  Task<Result<Game>> StartNewGame(string name, Player gameOwner);
}

public partial class Game
{
  public const string HasActiveGameError = "only_one_active_game_allowed";

  public class GameFactory(IGameDbContext gameDbContext) : IGameFactory
  {
    public async Task<Result<Game>> StartNewGame(string name, Player gameOwner)
    {
      var hasActiveGame = await gameDbContext.Games
        .AnyAsync(game => game.IsActive && game.CreatedBy.Id == gameOwner.Id);
      if (hasActiveGame)
      {
        return new Failure(HasActiveGameError);
      }

      var newGame = new Game
      {
        IsActive = true,
        Name = string.IsNullOrWhiteSpace(name) ?
          DateTimeOffset.UtcNow.ToString("o") :
          name,
        CreatedBy = gameOwner,
      };

      gameDbContext.Games.Add(newGame);

      newGame.AddDomainEvent(new NewGameStartedEvent(newGame));

      return newGame;
    }
  }
}
