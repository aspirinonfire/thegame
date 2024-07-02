using System;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public interface IGameFactory
{
  OneOf<Game, Failure> CreateNewGame(string name, Player gameOwner);
}

public partial class Game
{
  public class GameFactory(GameDbContext gameDbContext) : IGameFactory
  {
    public OneOf<Game, Failure> CreateNewGame(string name, Player gameOwner)
    {
      var newGame = new Game
      {
        IsActive = true,
        Name = string.IsNullOrWhiteSpace(name) ?
          DateTimeOffset.UtcNow.ToString("o") :
          name,
        CreatedBy = gameOwner,
      };

      gameDbContext.Games.Add(newGame);

      return newGame;
    }
  }
}
