using System;

namespace TheGame.Domain.DomainModels.Games;

public interface IGameFactory
{
  OneOf<Game, Failure> CreateNewGame(string name);
}

public partial class Game
{
  public class GameFactory : IGameFactory
  {
    public GameFactory()
    { }

    public OneOf<Game, Failure> CreateNewGame(string name)
    {
      var newGame = new Game
      {
        IsActive = true,
        Name = string.IsNullOrWhiteSpace(name) ?
          DateTimeOffset.UtcNow.ToString("o") :
          name
      };

      return newGame;
    }
  }
}
