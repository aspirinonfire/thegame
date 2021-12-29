using TheGame.Tests.TestUtils;
using Xunit;

namespace TheGame.Tests.Domain.Players
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class PlayerTests
  {
    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 2, false)]
    public void CanCompareTwoPlayers(long playerOneUserId, long playerTwoUserId, bool expectEqual)
    {
      var playerOne = new MockPlayer(null, playerOneUserId, "player one");
      var playerTwo = new MockPlayer(null, playerTwoUserId, "player two");

      Assert.Equal(expectEqual, playerOne == playerTwo);
      Assert.Equal(expectEqual, playerOne.GetHashCode() == playerTwo.GetHashCode());
    }
  }
}
