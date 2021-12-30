using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Players
{
  public partial class Player
  {
    public class PlayerFactory : IPlayerFactory
    {
      public PlayerFactory()
      {

      }

      public Result<Player> CreateNewPlayer(long userId, string name)
      {
        // Add player validations here

        return Result.Success<Player>(new Player
        {
          UserId = userId,
          Name = name
        });
      }
    }
  }
}
