using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerFactory
{
  DomainResult<Player> CreateNewPlayer(long userId, string name);
}

public partial class Player
{
  public class PlayerFactory : IPlayerFactory
  {
    public PlayerFactory()
    {

    }

    public DomainResult<Player> CreateNewPlayer(long userId, string name)
    {
      // Add player validations here

      return DomainResult.Success<Player>(new Player
      {
        UserId = userId,
        Name = name
      });
    }
  }
}
