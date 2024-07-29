namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerFactory
{
  Maybe<Player> CreateNewPlayer(string name);
}

public partial class Player
{
  public class PlayerFactory() : IPlayerFactory
  {
    public Maybe<Player> CreateNewPlayer(string name)
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
