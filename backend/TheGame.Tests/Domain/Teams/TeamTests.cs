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
        .Returns(expectedNewGame);

      var actualNewGameResult = uut.StartNewGame(gameFactory, gameName, player);

      actualNewGameResult.AssertIsSucceessful();

      var actualTeamGame = Assert.Single(uut.Games);
      Assert.Equal(expectedNewGame, actualTeamGame);
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
        .Returns(expectedNewGame);

      var actualNewGameResult = uut.StartNewGame(gameFactory, gameName, player);

      actualNewGameResult.AssertIsSucceessful();

      Assert.Equal(2, uut.Games.Count);
      Assert.Contains(existingFinishedGame, uut.Games);
      Assert.Contains(expectedNewGame, uut.Games);

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

      var actualNewGameResult = uut.StartNewGame(gameFactory, gameName, player);

      actualNewGameResult.AssertIsFailure(failureResult =>
      {
        Assert.Equal(Team.ErrorMessages.ActiveGameAlreadyExistsError, failureResult.ErrorMessage);
      });

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
        .Returns(existingActiveGame)
        .AndDoes(_ =>
        {
          existingActiveGame.SetActiveFlag(false);
        });

      var uut = new MockTeam([player],
        [existingActiveGame],
        name: "Test Team");

      var actualFinishGameResult = uut.FinishActiveGame(CommonMockedServices.GetSystemService(), player);

      actualFinishGameResult.AssertIsSucceessful();

      var actualGame = Assert.Single(uut.Games);
      Assert.Equal(existingActiveGame, actualGame);
      Assert.False(actualGame.IsActive);

      var actualEvent = Assert.Single(uut.DomainEvents);
      Assert.IsType<ExistingGameFinishedEvent>(actualEvent);
    }
  }
}
