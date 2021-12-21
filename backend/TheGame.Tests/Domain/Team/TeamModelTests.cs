using TheGame.Tests.TestUtils;
using Xunit;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.Team;
using Moq;
using TheGame.Tests.Domain.Player;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Tests.Domain.Game;
using TheGame.Domain.DomainModels.LicensePlate;
using System;
using TheGame.Domain.DomainModels.Team.Events;

namespace TheGame.Tests.Domain.Team
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class TeamModelTests
  {
    [Fact]
    public void CanStartFirstNewGameSuccessfully()
    {
      var player = new MockPlayerModel(null, 1, "test player");
      var gameName = "test game";

      var expectedNewGame = new Mock<GameModel>().Object;

      var uut = new MockTeamModel(new [] { player },
        Enumerable.Empty<GameModel>(),
        name: "Test Team");

      var gameFactory = new Mock<IGameFactory>();
      gameFactory
        .Setup(fac => fac.CreateNewGame(gameName))
        .Returns(Result.Success(expectedNewGame));

      var actual = uut.StartNewGame(gameFactory.Object, gameName, player);

      Assert.True(actual.IsSuccess);
      Assert.NotNull(actual.Value);
      Assert.Null(actual.ErrorMessage);
      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(expectedNewGame, actualGame);
      var actualEvent = Assert.Single(uut.DomainEvents);
      Assert.IsType<NewGameStartedEvent>(actualEvent);
    }

    [Fact]
    public void CanStartNewGameSuccessfullyWithFinishedGames()
    {
      var player = new MockPlayerModel(null, 1, "test player");
      var gameName = "test game";

      var existingFinishedGame = new MockGameModel(null,
        Enumerable.Empty<LicensePlateModel>(),
        "test game",
        false,
        null);

      var expectedNewGame = new Mock<GameModel>().Object;

      var uut = new MockTeamModel(new[] { player },
        new [] { existingFinishedGame },
        name: "Test Team");

      var gameFactory = new Mock<IGameFactory>();
      gameFactory
        .Setup(fac => fac.CreateNewGame(gameName))
        .Returns(Result.Success(expectedNewGame));

      var actual = uut.StartNewGame(gameFactory.Object, gameName, player);

      Assert.True(actual.IsSuccess);
      Assert.NotNull(actual.Value);
      Assert.Null(actual.ErrorMessage);
      Assert.Equal(2, uut.Games.Count());
      Assert.Equal(existingFinishedGame, uut.Games.First());
      Assert.Equal(expectedNewGame, uut.Games.Last());
      var actualEvent = Assert.Single(uut.DomainEvents);
      Assert.IsType<NewGameStartedEvent>(actualEvent);
    }

    [Fact]
    public void CanValidateExistingActiveGame()
    {
      var player = new MockPlayerModel(null, 1, "test player");
      var gameName = "test game";

      var existingActiveGame = new MockGameModel(null,
        Enumerable.Empty<LicensePlateModel>(),
        "test game",
        true,
        null);

      var uut = new MockTeamModel(new[] { player },
        new [] { existingActiveGame },
        name: "Test Team");

      var gameFactory = new Mock<IGameFactory>();

      var actual = uut.StartNewGame(gameFactory.Object, gameName, player);

      Assert.False(actual.IsSuccess);
      Assert.Null(actual.Value);
      Assert.Equal(TeamModel.ActiveGameAlreadyExistsError, actual.ErrorMessage);
      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(existingActiveGame, actualGame);
      gameFactory
        .Verify(fac => fac.CreateNewGame(It.IsAny<string>()), Times.Never);
      Assert.Empty(uut.DomainEvents);
    }

    [Fact]
    public void CanFinishActiveGame()
    {
      var player = new MockPlayerModel(null, 1, "test player");

      var existingActiveGame = new Mock<MockGameModel>(null,
        Enumerable.Empty<LicensePlateModel>(),
        "test game",
        true,
        null);

      existingActiveGame
        .Setup(game => game.FinishGame(It.IsAny<DateTimeOffset>()))
        .Callback<DateTimeOffset>(_ =>
        {
          existingActiveGame.Object.SetActiveFlag(false);
        })
        .Returns(Result.Success<GameModel>(existingActiveGame.Object));

      var uut = new MockTeamModel(new[] { player },
        new[] { existingActiveGame.Object },
        name: "Test Team");

      var actual = uut.FinishActiveGame(player);

      Assert.True(actual.IsSuccess);
      Assert.NotNull(actual.Value);
      Assert.Null(actual.ErrorMessage);
      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(existingActiveGame.Object, actualGame);
      Assert.False(actualGame.IsActive);
      var actualEvent = Assert.Single(uut.DomainEvents);
      Assert.IsType<ExistingGameFinishedEvent>(actualEvent);
    }
  }
}
