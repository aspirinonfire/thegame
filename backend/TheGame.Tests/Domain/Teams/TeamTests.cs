using TheGame.Tests.TestUtils;
using Xunit;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Teams;
using Moq;
using TheGame.Tests.Domain.Players;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Tests.Domain.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using System;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.Utils;

namespace TheGame.Tests.Domain.Teams
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class TeamTests
  {
    [Fact]
    public void CanStartFirstNewGameSuccessfully()
    {
      var player = new MockPlayer(null, 1, "test player");
      var gameName = "test game";

      var expectedNewGame = new Mock<Game>().Object;

      var uut = new MockTeam(new [] { player },
        Enumerable.Empty<Game>(),
        name: "Test Team");

      var gameFactory = new Mock<IGameFactory>();
      gameFactory
        .Setup(fac => fac.CreateNewGame(gameName))
        .Returns(DomainResult.Success(expectedNewGame));

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
      var player = new MockPlayer(null, 1, "test player");
      var gameName = "test game";

      var existingFinishedGame = new MockGame(Enumerable.Empty<GameLicensePlate>(),
        false,
        null,
        gameName);

      var expectedNewGame = new Mock<Game>().Object;

      var uut = new MockTeam(new[] { player },
        new [] { existingFinishedGame },
        name: "Test Team");

      var gameFactory = new Mock<IGameFactory>();
      gameFactory
        .Setup(fac => fac.CreateNewGame(gameName))
        .Returns(DomainResult.Success(expectedNewGame));

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
      var player = new MockPlayer(null, 1, "test player");
      var gameName = "test game";

      var existingActiveGame = new MockGame(Enumerable.Empty<GameLicensePlate>(),
        true,
        null,
        gameName);

      var uut = new MockTeam(new[] { player },
        new [] { existingActiveGame },
        name: "Test Team");

      var gameFactory = new Mock<IGameFactory>();

      var actual = uut.StartNewGame(gameFactory.Object, gameName, player);

      Assert.False(actual.IsSuccess);
      Assert.Null(actual.Value);
      Assert.Equal(Team.ErrorMessages.ActiveGameAlreadyExistsError, actual.ErrorMessage);
      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(existingActiveGame, actualGame);
      gameFactory
        .Verify(fac => fac.CreateNewGame(It.IsAny<string>()), Times.Never);
      Assert.Empty(uut.DomainEvents);
    }

    [Fact]
    public void CanFinishActiveGame()
    {
      var player = new MockPlayer(null, 1, "test player");

      var existingActiveGame = new Mock<MockGame>(Enumerable.Empty<GameLicensePlate>(),
        true,
        null,
        "Active game");

      existingActiveGame
        .Setup(game => game.FinishGame(It.IsAny<DateTimeOffset>()))
        .Callback<DateTimeOffset>(_ =>
        {
          existingActiveGame.Object.SetActiveFlag(false);
        })
        .Returns(DomainResult.Success<Game>(existingActiveGame.Object));

      var uut = new MockTeam(new[] { player },
        new[] { existingActiveGame.Object },
        name: "Test Team");

      var actual = uut.FinishActiveGame(CommonMockedServices.GetSystemService(), player);

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