namespace TheGame.Domain.DomainModels.Players;

public interface IPlayerFactory
{
  Result<Player> CreateNewPlayer(string name);
}

public partial class Player
{
  public class PlayerFactory() : IPlayerFactory
  {
    public Result<Player> CreateNewPlayer(string name)
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
