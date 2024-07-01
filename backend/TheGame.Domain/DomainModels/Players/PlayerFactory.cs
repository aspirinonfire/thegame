namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerFactory
{
  OneOf<Player, Failure> CreateNewPlayer(long userId, string name);
}

public partial class Player
{
  public class PlayerFactory : IPlayerFactory
  {
    public PlayerFactory()
    {

    }

    public OneOf<Player, Failure> CreateNewPlayer(long userId, string name)
    {
      // Add player validations here

      return new Player
      {
        UserId = userId,
        Name = name
      };
    }
  }
}
