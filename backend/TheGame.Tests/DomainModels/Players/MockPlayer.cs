using TheGame.Domain.DomainModels.Players;

namespace TheGame.Tests.DomainModels.Players
{
  public class MockPlayer : Player
  {
    public MockPlayer(long playerId, string name)
    {
      Id = playerId;

      Name = name;
    }
  }
}
