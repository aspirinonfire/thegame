using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Teams;
using TheGame.Tests.Domain.Games;
using TheGame.Tests.Domain.Players;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.Domain.Teams
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class TeamTests
  {
    [Fact]
    public void WillStartFirstNewGameSuccessfully()
    {
      var player = new MockPlayer(null, 1, "test player");
      var gameName = "test game";

      var expectedNewGame = Substitute.For<Game>();

      var uut = new MockTeam([player],
        [],
        name: "Test Team");

      var gameFactory = Substitute.For<IGameFactory>();
      gameFactory
        .CreateNewGame(gameName)
        .Returns(DomainResult.Success(expectedNewGame));

      var actual = uut.StartNewGame(gameFactory, gameName, player);

      Assert.True(actual.IsSuccess);
      Assert.NotNull(actual.Value);
      Assert.Null(actual.ErrorMessage);
      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(expectedNewGame, actualGame);
      var actualEvent = Assert.Single(uut.DomainEvents);
      Assert.IsType<NewGameStartedEvent>(actualEvent);
    }

    [Fact]
    public void WillStartNewGameSuccessfullyWithFinishedGames()
    {
      var player = new MockPlayer(null, 1, "test player");
      var gameName = "test game";

      var existingFinishedGame = new MockGame(Enumerable.Empty<GameLicensePlate>(),
        false,
        null,
        gameName);

      var expectedNewGame = Substitute.For<Game>();

      var uut = new MockTeam([player],
        [existingFinishedGame],
        name: "Test Team");

      var gameFactory = Substitute.For<IGameFactory>();
      gameFactory
        .CreateNewGame(gameName)
        .Returns(DomainResult.Success(expectedNewGame));

      var actual = uut.StartNewGame(gameFactory, gameName, player);

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
    public void WillReturnGameAlreadyExistsErrorWhenCreatingNewGame()
    {
      var player = new MockPlayer(null, 1, "test player");
      var gameName = "test game";

      var existingActiveGame = new MockGame(Enumerable.Empty<GameLicensePlate>(),
        true,
        null,
        gameName);

      var uut = new MockTeam([player],
        [existingActiveGame],
        name: "Test Team");

      var gameFactory = Substitute.For<IGameFactory>();

      var actual = uut.StartNewGame(gameFactory, gameName, player);

      Assert.False(actual.IsSuccess);
      Assert.Null(actual.Value);
      Assert.Equal(Team.ErrorMessages.ActiveGameAlreadyExistsError, actual.ErrorMessage);

      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(existingActiveGame, actualGame);

      gameFactory.Received(0).CreateNewGame(Arg.Any<string>());
      Assert.Empty(uut.DomainEvents);
    }

    [Fact]
    public void CanFinishActiveGame()
    {
      var player = new MockPlayer(null, 1, "test player");

      var existingActiveGame = Substitute.For<MockGame>(Enumerable.Empty<GameLicensePlate>(),
        true,
        null,
        "Active game");

      existingActiveGame
        .FinishGame(Arg.Any<DateTimeOffset>())
        .Returns(DomainResult.Success<Game>(existingActiveGame))
        .AndDoes(_ =>
        {
          existingActiveGame.SetActiveFlag(false);
        });

      var uut = new MockTeam([player],
        [existingActiveGame],
        name: "Test Team");

      var actual = uut.FinishActiveGame(CommonMockedServices.GetSystemService(), player);

      Assert.True(actual.IsSuccess);
      Assert.NotNull(actual.Value);
      Assert.Null(actual.ErrorMessage);

      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(existingActiveGame, actualGame);
      Assert.False(actualGame.IsActive);

      var actualEvent = Assert.Single(uut.DomainEvents);
      Assert.IsType<ExistingGameFinishedEvent>(actualEvent);
    }
  }
}
