using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Tests.DomainModels.LicensePlates;
using TheGame.Tests.DomainModels.Players;

namespace TheGame.Tests.DomainModels.Games
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class GameTests
  {
    [Fact]
    public void WillSpotNewPlate()
    {
      var spottedBy = new MockPlayer(1, "test");

      var gameLpSpots = new GameLicensePlateSpots([(Country.US, StateOrProvince.CA)],
        CommonMockedServices.DefaultTestDate,
        spottedBy);

      var lpSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
      licensePlateFactory
        .CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, Arg.Any<DateTimeOffset>())
        .Returns(callInfo =>
        {
          var newPlateSpot = new MockLicensePlate(callInfo.ArgAt<Country>(0), callInfo.ArgAt<StateOrProvince>(1));
          return new MockGameLicensePlate(newPlateSpot,
            callInfo.ArgAt<Player>(2));
        });

      var uut = new MockGame(licensePlates: null,
        createdBy: spottedBy,
        isActive: true);

      var actualSpotResult = uut.UpdateLicensePlateSpots(licensePlateFactory,
        CommonMockedServices.GetSystemService(),
        gameLpSpots);

      actualSpotResult.AssertIsSucceessful();

      var actualLicensePlateSpot = Assert.Single(uut.GameLicensePlates);
      Assert.Equal(lpSpot.LicensePlate, actualLicensePlateSpot.LicensePlate);
      Assert.Equal(spottedBy, actualLicensePlateSpot.SpottedBy);
      Assert.Equal(CommonMockedServices.DefaultTestDate, actualLicensePlateSpot.DateCreated);

      var actualDomainEvent = Assert.Single(uut.DomainEvents);
      var actualSpottedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualDomainEvent);
      var actualSpotFromEvent = Assert.Single(actualSpottedEvent.LicensePlateSpotModels);
      Assert.Equal(actualLicensePlateSpot, actualSpotFromEvent);
    }

    [Fact]
    public void WillRemovePreviouslySpottedPlates()
    {
      var spottedBy = new MockPlayer(2, "existing");

      var gameLpSpots = new GameLicensePlateSpots([],
        CommonMockedServices.DefaultTestDate,
        spottedBy);

      var existingSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var uut = new MockGame(licensePlates: [existingSpot],
        createdBy: spottedBy,
        isActive: true);

      var actualSpotResult = uut.UpdateLicensePlateSpots(null!,
        CommonMockedServices.GetSystemService(),
        gameLpSpots);

      actualSpotResult.AssertIsSucceessful();

      Assert.Empty(uut.GameLicensePlates);
      var actualEvent = Assert.Single(uut.DomainEvents);
      var actualSpottedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualEvent);
      Assert.Empty(actualSpottedEvent.LicensePlateSpotModels);
    }

    [Fact]
    public void WillHandleAlreadySpottedPlate()
    {
      var existingSpottedBy = new MockPlayer(2, "existing");
      var existingSpot = new MockGameLicensePlate(
  new MockLicensePlate(Country.US, StateOrProvince.CA),
        existingSpottedBy);

      var spottedBy = new MockPlayer(1, "test");
      var gameLpSpots = new GameLicensePlateSpots([(Country.US, StateOrProvince.CA)],
        new DateTimeOffset(2024, 7, 11, 16, 0, 0, TimeSpan.Zero),
        spottedBy);

      var newSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
      licensePlateFactory
        .CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultTestDate)
        .Returns(newSpot);

      var uut = new MockGame([existingSpot],
        createdBy: spottedBy,
        isActive: true);

      uut.AddInvitedPlayer(existingSpottedBy);

      var actualSpotResult = uut.UpdateLicensePlateSpots(licensePlateFactory,
        CommonMockedServices.GetSystemService(),
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
      var gameLpSpots = new GameLicensePlateSpots([(Country.US, StateOrProvince.CA)],
        new DateTimeOffset(2024, 7, 11, 16, 0, 0, TimeSpan.Zero),
        spottedBy);

      var lpSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
      licensePlateFactory
        .CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultTestDate)
        .Returns(lpSpot);

      var uut = new MockGame(licensePlates: null,
        createdBy: spottedBy,
        isActive: false);

      var actualSpotResult = uut.UpdateLicensePlateSpots(licensePlateFactory,
        CommonMockedServices.GetSystemService(),
        gameLpSpots);

      actualSpotResult.AssertIsFailure(actualFailure => Assert.Equal(Game.ErrorMessages.InactiveGameError, actualFailure.ErrorMessage));

      Assert.Empty(uut.GameLicensePlates);
      Assert.Empty(uut.DomainEvents);
    }
  }
}
