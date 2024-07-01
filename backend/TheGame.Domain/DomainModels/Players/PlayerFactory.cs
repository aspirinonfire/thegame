namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerFactory
{
  OneOf<Player, Failure> CreateNewPlayer(string name);
}

public partial class Player
{
  public class PlayerFactory : IPlayerFactory
  {
    public OneOf<Player, Failure> CreateNewPlayer(string name)
    {
      // TODO Add player validations here

      var newPlayer = new Player
      {
        Name = name,
      };

      return newPlayer;
    }
  }
}
