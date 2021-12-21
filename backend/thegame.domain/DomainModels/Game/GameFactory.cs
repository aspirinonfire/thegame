using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Game
{
  public partial class GameModel
  {
    public class GameFactory : IGameFactory
    {
      public GameFactory()
      {
        // TODO add dependencies
      }

      public Result<GameModel> CreateNewGame(string name)
      {
        var newGame = new GameModel
        {
          IsActive = true,
          Name = string.IsNullOrWhiteSpace(name) ?
            DateTimeOffset.UtcNow.ToString("o") :
            name
        };

        // TODO track new entity

        return Result.Success(newGame);
      }
    }
  }
}
