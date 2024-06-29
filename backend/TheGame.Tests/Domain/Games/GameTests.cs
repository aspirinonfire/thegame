using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Tests.Domain.LicensePlates;
using TheGame.Tests.Domain.Players;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.Domain.Games
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class GameTests
  {
    [Fact]
    public void WillSpotNewPlate()
    {
      var spottedBy = new MockPlayer(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var lpSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
      licensePlateFactory
        .CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultDate)
        .Returns(DomainResult.Success<GameLicensePlate>(lpSpot));

      var uut = new MockGame(null, true, null);

      var actual = uut.AddLicensePlateSpot(licensePlateFactory,
        CommonMockedServices.GetSystemService(),
        [toSpot],
        spottedBy);

      Assert.True(actual.IsSuccess);

      var actualLicensePlateSpot = Assert.Single(uut.GameLicensePlates);
      Assert.Equal(lpSpot, actualLicensePlateSpot);

      var actualDomainEvent = Assert.Single(uut.DomainEvents);
      var actualLicensePlateSpotAddedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualDomainEvent);
      var actualSpotFromEvent = Assert.Single(actualLicensePlateSpotAddedEvent.LicensePlateSpotModels);
      Assert.Equal(lpSpot, actualSpotFromEvent);
    }

    [Fact]
    public void WillRemovePreviouslySpottedPlates()
    {
      var existingSpottedBy = new MockPlayer(null, 2, "existing");
      var toRemove = (Country.US, StateOrProvince.CA);

      var existingSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        existingSpottedBy);

      var uut = new MockGame(new[] { existingSpot }, true, null);

      var actual = uut.RemoveLicensePlateSpot(new[] { toRemove },
        existingSpottedBy);

      Assert.True(actual.IsSuccess);
      Assert.Empty(uut.GameLicensePlates);
      var actualEvent = Assert.Single(uut.DomainEvents);
      var actualRemovedEvent = Assert.IsType<LicensePlateSpotRemovedEvent>(actualEvent);
      var removedPlate = Assert.Single(actualRemovedEvent.LicensePlatesToRemove);
      Assert.Equal((existingSpot.LicensePlate.Country, existingSpot.LicensePlate.StateOrProvince),
        removedPlate);
    }

    [Fact]
    public void WillHandleAlreadySpottedPlate()
    {
      var existingSpottedBy = new MockPlayer(null, 2, "existing");
      var spottedBy = new MockPlayer(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var existingSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        existingSpottedBy);

      var newSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
      licensePlateFactory
        .CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultDate)
        .Returns(DomainResult.Success<GameLicensePlate>(newSpot));

      var uut = new MockGame([existingSpot], true, null);

      var actual = uut.AddLicensePlateSpot(licensePlateFactory,
        CommonMockedServices.GetSystemService(),
        [toSpot],
        spottedBy);

      Assert.True(actual.IsSuccess);
      var actualLicensePlateSpot = Assert.Single(uut.GameLicensePlates);
      Assert.Equal(existingSpot, actualLicensePlateSpot);
      Assert.Equal(existingSpottedBy, actualLicensePlateSpot.SpottedBy);
      Assert.Empty(uut.DomainEvents);
    }

    [Fact]
    public void WillReturnInactiveGameErrorOnSpottingPlateForHistoricGameRecord()
    {
      var spottedBy = new MockPlayer(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var lpSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var licensePlateFactory = Substitute.For<IGameLicensePlateFactory>();
      licensePlateFactory
        .CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultDate)
        .Returns(DomainResult.Success<GameLicensePlate>(lpSpot));

      var uut = new MockGame(null, false, null);

      var actual = uut.AddLicensePlateSpot(licensePlateFactory,
        CommonMockedServices.GetSystemService(),
        [toSpot],
        spottedBy);

      Assert.False(actual.IsSuccess);
      Assert.Equal(Game.ErrorMessages.InactiveGameError, actual.ErrorMessage);
      Assert.Empty(uut.GameLicensePlates);
      Assert.Empty(uut.DomainEvents);
    }
  }
}
