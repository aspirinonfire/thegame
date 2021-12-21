using TheGame.Tests.TestUtils;
using Xunit;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.Team;
using Moq;
using TheGame.Tests.Domain.Player;
using System.Linq;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Tests.Domain.Team
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class TeamServiceTests
  {
    [Fact]
    public void CanCreateNewGame()
    {
      // TODO add helper methods
      var player = new MockPlayerModel(null, 1, "test player");
      var gameName = "test game";

      var expectedNewGame = new Mock<GameModel>().Object;

      var uut = new MockTeamModel(new [] { player },
        Enumerable.Empty<GameModel>(),
        id: 1,
        name: "Test Team");

      var gameFactory = new Mock<IGameFactory>();
      gameFactory
        .Setup(fac => fac.CreateNewGame(gameName))
        .Returns(Result.Success(expectedNewGame));

      var actual = uut.AddNewGame(gameFactory.Object, gameName, player);

      Assert.True(actual.IsSuccess);
      Assert.NotNull(actual.Value);
      Assert.Null(actual.ErrorMessage);
      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(expectedNewGame, actualGame);
    }
  }
}
