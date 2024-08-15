using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Tests.DomainModels.LicensePlates;
using TheGame.Tests.DomainModels.Players;

namespace TheGame.Tests.DomainModels.Games;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class GameTests
{
  [Fact]
  public void WillSpotNewPlate()
  {
    var spottedBy = new MockPlayer(1, "test");

    var gameLpSpots = new GameLicensePlateSpots([new (Country.US, StateOrProvince.CA)],
      spottedBy);

    var lpSpot = new MockGameLicensePlate(
      new MockLicensePlate(new (Country.US, StateOrProvince.CA)),
      spottedBy);

    var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
    licensePlateFactory
      .CreateLicensePlateSpot(Arg.Any<LicensePlate.PlateKey>(), spottedBy, Arg.Any<DateTimeOffset>())
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

    var uut = new MockGame(licensePlates: null,
      createdBy: spottedBy,
      isActive: true);

    var actualSpotResult = uut.UpdateLicensePlateSpots(licensePlateFactory,
      CommonMockedServices.GetSystemService(),
      calculator,
      Substitute.For<IGameDbContext>(),
      gameLpSpots);

    actualSpotResult.AssertIsSucceessful();

    var actualLicensePlateSpot = Assert.Single(uut.GameLicensePlates);
    Assert.Equal(lpSpot.LicensePlate.Id, actualLicensePlateSpot.LicensePlate.Id);
    Assert.Equal(spottedBy, actualLicensePlateSpot.SpottedBy);
    Assert.Equal(CommonMockedServices.DefaultTestDate, actualLicensePlateSpot.DateCreated);

    var actualDomainEvent = Assert.Single(uut.DomainEvents);
    var actualSpottedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualDomainEvent);
    var actualSpotFromEvent = Assert.Single(actualSpottedEvent.Game.GameLicensePlates);
    Assert.Equal(actualLicensePlateSpot, actualSpotFromEvent);
  }

  [Fact]
  public void WillUpdateScoreOnSpottedNewPlate()
  {
    var spottedBy = new MockPlayer(1, "test");

    var gameLpSpots = new GameLicensePlateSpots([new(Country.US, StateOrProvince.CA)],
      spottedBy);

    var lpSpot = new MockGameLicensePlate(
      new MockLicensePlate(new (Country.US, StateOrProvince.CA)),
      spottedBy);

    var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
    licensePlateFactory
      .CreateLicensePlateSpot(Arg.Any<LicensePlate.PlateKey>(), spottedBy, Arg.Any<DateTimeOffset>())
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

    var uut = new MockGame(licensePlates: null,
      createdBy: spottedBy,
      isActive: true);

    var actualSpotResult = uut.UpdateLicensePlateSpots(licensePlateFactory,
      CommonMockedServices.GetSystemService(),
      calculator,
      Substitute.For<IGameDbContext>(),
      gameLpSpots);

    actualSpotResult.AssertIsSucceessful(actualUpdatedGame =>
    {
      var actualAchievement = Assert.Single(actualUpdatedGame.GameScore.Achievements);
      Assert.Equal("Test Achievement", actualAchievement);
      Assert.Equal(100, actualUpdatedGame.GameScore.TotalScore);
    });

  }

  [Fact]
  public void WillRemovePreviouslySpottedPlates()
  {
    var spottedBy = new MockPlayer(2, "existing");

    var gameLpSpots = new GameLicensePlateSpots([], spottedBy);

    var existingSpot = new MockGameLicensePlate(
      new MockLicensePlate(new (Country.US, StateOrProvince.CA)),
      spottedBy);

    var uut = new MockGame(licensePlates: [existingSpot],
      createdBy: spottedBy,
      isActive: true);

    var calculator = Substitute.For<IGameScoreCalculator>();
    calculator
      .CalculateGameScore(Arg.Any<IReadOnlyCollection<LicensePlate.PlateKey>>())
      .Returns(new GameScoreResult(0, [], 0));

    var actualSpotResult = uut.UpdateLicensePlateSpots(null!,
      CommonMockedServices.GetSystemService(),
      calculator,
      Substitute.For<IGameDbContext>(),
      gameLpSpots);

    actualSpotResult.AssertIsSucceessful();

    Assert.Empty(uut.GameLicensePlates);
    var actualEvent = Assert.Single(uut.DomainEvents);
    var actualSpottedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualEvent);
    Assert.Empty(actualSpottedEvent.Game.GameLicensePlates);
  }

  [Fact]
  public void WillHandleAlreadySpottedPlate()
  {
    var existingSpottedBy = new MockPlayer(2, "existing");
    var existingSpot = new MockGameLicensePlate(
new MockLicensePlate(new (Country.US, StateOrProvince.CA)),
      existingSpottedBy);

    var spottedBy = new MockPlayer(1, "test");
    var gameLpSpots = new GameLicensePlateSpots([new(Country.US, StateOrProvince.CA)],
      spottedBy);

    var newSpot = new MockGameLicensePlate(
      new MockLicensePlate(new (Country.US, StateOrProvince.CA)),
      spottedBy);

    var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
    licensePlateFactory
      .CreateLicensePlateSpot(Arg.Any<LicensePlate.PlateKey>(), spottedBy, CommonMockedServices.DefaultTestDate)
      .Returns(newSpot);

    var calculator = Substitute.For<IGameScoreCalculator>();
    calculator
      .CalculateGameScore(Arg.Any<IReadOnlyCollection<LicensePlate.PlateKey>>())
      .Returns(new GameScoreResult(0, [], 0));

    var uut = new MockGame([existingSpot],
      createdBy: spottedBy,
      isActive: true);

    uut.AddInvitedPlayer(existingSpottedBy);

    var actualSpotResult = uut.UpdateLicensePlateSpots(licensePlateFactory,
      CommonMockedServices.GetSystemService(),
      calculator,
      Substitute.For<IGameDbContext>(),
      gameLpSpots);

    actualSpotResult.AssertIsSucceessful();

    var actualLicensePlateSpot = Assert.Single(uut.GameLicensePlates);
    Assert.Equal(existingSpot, actualLicensePlateSpot);
    Assert.Equal(existingSpottedBy, actualLicensePlateSpot.SpottedBy);
    Assert.Empty(uut.DomainEvents);
  }

  [Fact]
  public void WillReturnInactiveGameErrorOnSpottingPlateForHistoricGameRecord()
  {
    var spottedBy = new MockPlayer(1, "test");
    var gameLpSpots = new GameLicensePlateSpots([new (Country.US, StateOrProvince.CA)], spottedBy);

    var lpSpot = new MockGameLicensePlate(
      new MockLicensePlate(new (Country.US, StateOrProvince.CA)),
      spottedBy);

    var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
    licensePlateFactory
      .CreateLicensePlateSpot(Arg.Any<LicensePlate.PlateKey>(), spottedBy, CommonMockedServices.DefaultTestDate)
      .Returns(lpSpot);

    var calculator = Substitute.For<IGameScoreCalculator>();
    calculator
      .CalculateGameScore(Arg.Any<IReadOnlyCollection<LicensePlate.PlateKey>>())
      .Returns(new GameScoreResult(0, [], 0));

    var uut = new MockGame(licensePlates: null,
      createdBy: spottedBy,
      isActive: false);

    var actualSpotResult = uut.UpdateLicensePlateSpots(licensePlateFactory,
      CommonMockedServices.GetSystemService(),
      calculator,
      Substitute.For<IGameDbContext>(),
      gameLpSpots);

    actualSpotResult.AssertIsFailure(actualFailure => Assert.Equal(ErrorMessageProvider.InactiveGameError, actualFailure.ErrorMessage));

    Assert.Empty(uut.GameLicensePlates);
    Assert.Empty(uut.DomainEvents);
  }
}
