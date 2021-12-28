using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Games
{
  public partial class Game
  {
    public class GameFactory : IGameFactory
    {
      public GameFactory()
      {
        // TODO add dependencies
      }

      public Result<Game> CreateNewGame(string name)
      {
        var newGame = new Game
        {
          IsActive = true,
          Name = string.IsNullOrWhiteSpace(name) ?
            DateTimeOffset.UtcNow.ToString("o") :
            name
        };

        return Result.Success(newGame);
      }
    }
  }
}
