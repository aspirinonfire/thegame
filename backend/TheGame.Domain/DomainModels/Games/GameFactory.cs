using System;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Games
{
  public partial class Game
  {
    public class GameFactory : IGameFactory
    {
      public GameFactory()
      { }

      public DomainResult<Game> CreateNewGame(string name)
      {
        var newGame = new Game
        {
          IsActive = true,
          Name = string.IsNullOrWhiteSpace(name) ?
            DateTimeOffset.UtcNow.ToString("o") :
            name
        };

        return DomainResult.Success(newGame);
      }
    }
  }
}