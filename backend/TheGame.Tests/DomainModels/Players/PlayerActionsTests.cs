using MockQueryable;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Tests.DomainModels.Games;
using TheGame.Tests.DomainModels.LicensePlates;

namespace TheGame.Tests.DomainModels.Players;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class PlayerActionsTests
{
  [Fact]
  public async Task WillSpotNewPlateAsync()
  {
    var actingPlayer = new MockPlayer(1, "test");
    var testGame = new MockGame(1, [], actingPlayer, true);
    actingPlayer.SetOwnedGames([testGame]);

    var gameLpSpots = new[]
    {
      new LicensePlate.PlateKey(Country.US, StateOrProvince.CA)
    };

    var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
    licensePlateFactory
      .CreateLicensePlateSpot(Arg.Any<LicensePlate.PlateKey>(), actingPlayer, Arg.Any<DateTimeOffset>())
      .Returns(callInfo =>
      {
        var newPlateSpot = new MockLicensePlate(callInfo.ArgAt<LicensePlate.PlateKey>(0));
        return new MockGameLicensePlate(newPlateSpot,
          callInfo.ArgAt<Player>(1));
      });

    var calculator = Substitute.For<IGameScoreCalculator>();
    calculator
      .CalculateGameScore(Arg.Any<IReadOnlyCollection<LicensePlate.PlateKey>>())
      .Returns(new GameScoreResult(0, [], 0));

    var uut = new Player.PlayerActions(Substitute.For<IGameDbContext>(),
      calculator,
      Substitute.For<IPlayerFactory>(),
      CommonMockedServices.GetMockedTimeProvider(),
      licensePlateFactory,
      new [] { actingPlayer }.BuildMock());

    var actualSpotResult = await uut.UpdateLicensePlateSpots(1, gameLpSpots);

    var actualGame = actualSpotResult.AssertIsSucceessful();

    var actualLicensePlateSpot = Assert.Single(actualGame.GameLicensePlates);
    Assert.Equal(Country.US, actualLicensePlateSpot.LicensePlate.Country);
    Assert.Equal(StateOrProvince.CA, actualLicensePlateSpot.LicensePlate.StateOrProvince);
    Assert.Equal(actingPlayer, actualLicensePlateSpot.SpottedBy);
    Assert.Equal(CommonMockedServices.DefaultTestDate, actualLicensePlateSpot.DateCreated);

    var actualDomainEvent = Assert.Single(actingPlayer.DomainEvents);
    var actualSpottedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualDomainEvent);
    var actualSpotFromEvent = Assert.Single(actualSpottedEvent.Game.GameLicensePlates);
    Assert.Equal(actualLicensePlateSpot, actualSpotFromEvent);
  }

  [Fact]
  public async Task WillUpdateScoreOnSpottedNewPlateAsync()
  {
    var actingPlayer = new MockPlayer(1, "test");
    var testGame = new MockGame(1, [], actingPlayer, true);
    actingPlayer.SetOwnedGames([testGame]);

    var gameLpSpots = new[]
    {
      new LicensePlate.PlateKey(Country.US, StateOrProvince.CA)
    };

    var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
    licensePlateFactory
      .CreateLicensePlateSpot(Arg.Any<LicensePlate.PlateKey>(), actingPlayer, Arg.Any<DateTimeOffset>())
      .Returns(callInfo =>
      {
        var newPlateSpot = new MockLicensePlate(callInfo.ArgAt<LicensePlate.PlateKey>(0));
        return new MockGameLicensePlate(newPlateSpot,
          callInfo.ArgAt<Player>(1));
      });

    var calculator = Substitute.For<IGameScoreCalculator>();
    calculator
      .CalculateGameScore(Arg.Any<IReadOnlyCollection<LicensePlate.PlateKey>>())
      .Returns(new GameScoreResult(0, ["Test Achievement"], 100));

    var uut = new Player.PlayerActions(Substitute.For<IGameDbContext>(),
      calculator,
      Substitute.For<IPlayerFactory>(),
      CommonMockedServices.GetMockedTimeProvider(),
      licensePlateFactory,
      new[] { actingPlayer }.BuildMock());

    var actualSpotResult = await uut.UpdateLicensePlateSpots(1, gameLpSpots);

    var actualUpdatedGame = actualSpotResult.AssertIsSucceessful();
    var actualAchievement = Assert.Single(actualUpdatedGame.GameScore.Achievements);
    Assert.Equal("Test Achievement", actualAchievement);
    Assert.Equal(100, actualUpdatedGame.GameScore.TotalScore);
  }

  [Fact]
  public async Task WillRemovePreviouslySpottedPlatesAsync()
  {
    var existingSpot = new MockGameLicensePlate(
      new MockLicensePlate(new(Country.US, StateOrProvince.CA)),
      new MockPlayer());

    var actingPlayer = new MockPlayer(1, "test");
    var testGame = new MockGame(1, [existingSpot], actingPlayer, true);
    actingPlayer.SetOwnedGames([testGame]);

    var gameLpSpots = Array.Empty<LicensePlate.PlateKey>();

    var calculator = Substitute.For<IGameScoreCalculator>();
    calculator
      .CalculateGameScore(Arg.Any<IReadOnlyCollection<LicensePlate.PlateKey>>())
      .Returns(new GameScoreResult(0, [], 0));

    var uut = new Player.PlayerActions(Substitute.For<IGameDbContext>(),
      calculator,
      Substitute.For<IPlayerFactory>(),
      CommonMockedServices.GetMockedTimeProvider(),
      Substitute.For<IGameLicensePlateFactory>(),
      new[] { actingPlayer }.BuildMock());

    var actualSpotResult = await uut.UpdateLicensePlateSpots(1, gameLpSpots);

    var actualGame = actualSpotResult.AssertIsSucceessful();

    Assert.Empty(actualGame.GameLicensePlates);
    var actualEvent = Assert.Single(actingPlayer.DomainEvents);
    var actualSpottedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualEvent);
    Assert.Empty(actualSpottedEvent.Game.GameLicensePlates);
  }

  [Fact]
  public async Task WillHandleAlreadySpottedPlateAsync()
  {
    var existingSpottedBy = new MockPlayer(2, "existing");

    var existingSpot = new MockGameLicensePlate(
      new MockLicensePlate(new(Country.US, StateOrProvince.CA)),
      existingSpottedBy);

    var actingPlayer = new MockPlayer(1, "test");
    var testGame = new MockGame(1, [existingSpot], actingPlayer, true);
    testGame.AddInvitedPlayer(existingSpottedBy);
    actingPlayer.SetOwnedGames([testGame]);

    var gameLpSpots = new[]
    {
      new LicensePlate.PlateKey(Country.US, StateOrProvince.CA)
    };

    var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
    licensePlateFactory
      .CreateLicensePlateSpot(Arg.Any<LicensePlate.PlateKey>(), actingPlayer, CommonMockedServices.DefaultTestDate)
      .Returns(new MockGameLicensePlate(
        new MockLicensePlate(new(Country.US, StateOrProvince.CA)),
        actingPlayer));

    var calculator = Substitute.For<IGameScoreCalculator>();
    calculator
      .CalculateGameScore(Arg.Any<IReadOnlyCollection<LicensePlate.PlateKey>>())
      .Returns(new GameScoreResult(0, [], 0));

    var uut = new Player.PlayerActions(Substitute.For<IGameDbContext>(),
      calculator,
      Substitute.For<IPlayerFactory>(),
      CommonMockedServices.GetMockedTimeProvider(),
      Substitute.For<IGameLicensePlateFactory>(),
      new[] { actingPlayer }.BuildMock());

    var actualSpotResult = await uut.UpdateLicensePlateSpots(1, gameLpSpots);

    var actualGame = actualSpotResult.AssertIsSucceessful();

    var actualLicensePlateSpot = Assert.Single(actualGame.GameLicensePlates);
    Assert.Equal(existingSpot, actualLicensePlateSpot);
    Assert.Equal(existingSpottedBy, actualLicensePlateSpot.SpottedBy);
    Assert.Empty(actingPlayer.DomainEvents);
  }

  [Fact]
  public async Task WillReturnInactiveGameErrorOnSpottingPlateForHistoricGameRecordAsync()
  {
    var actingPlayer = new MockPlayer(1, "test");
    var testGame = new MockGame(1, [], actingPlayer, false);
    actingPlayer.SetOwnedGames([testGame]);

    var gameLpSpots = new[]
    {
      new LicensePlate.PlateKey(Country.US, StateOrProvince.CA)
    };

    var uut = new Player.PlayerActions(Substitute.For<IGameDbContext>(),
      Substitute.For<IGameScoreCalculator>(),
      Substitute.For<IPlayerFactory>(),
      CommonMockedServices.GetMockedTimeProvider(),
      Substitute.For<IGameLicensePlateFactory>(),
      new[] { actingPlayer }.BuildMock());

    var actualSpotResult = await uut.UpdateLicensePlateSpots(1, gameLpSpots);

    actualSpotResult.AssertIsFailure(actualFailure => Assert.Equal(ErrorMessageProvider.InactiveGameError, actualFailure.ErrorMessage));

    Assert.Empty(testGame.GameLicensePlates);
    Assert.Empty(actingPlayer.DomainEvents);
  }
}
