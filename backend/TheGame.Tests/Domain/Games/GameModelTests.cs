using Moq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Tests.Domain.LicensePlates;
using TheGame.Tests.Domain.Players;
using TheGame.Tests.TestUtils;
using Xunit;

namespace TheGame.Tests.Domain.Games
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class GameModelTests
  {
    [Fact]
    public void CanSpotPlate()
    {
      var spottedBy = new MockPlayerModel(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var lpSpot = new MockLicensePlateSpotModel(
        new MockLicensePlateModel(Country.US, StateOrProvince.CA),
        spottedBy);

      var lpFactory = new Mock<ILicensePlateSpotFactory>();
      lpFactory
        .Setup(fac => fac.SpotLicensePlate(Country.US, StateOrProvince.CA, spottedBy))
        .Returns(Result.Success<LicensePlateSpot>(lpSpot));

      var uut = new MockGameModel(null, null, true, null);

      var actual = uut.AddLicensePlateSpot(lpFactory.Object,
        new[] { toSpot },
        spottedBy);

      Assert.True(actual.IsSuccess);
      var actualLicensePlateSpot = Assert.Single(uut.LicensePlateSpots);
      Assert.Equal(lpSpot, actualLicensePlateSpot);
      var actualDomainEvent = Assert.Single(uut.DomainEvents);
      var actualLicensePlateSpotAddedEvent = Assert.IsType<LicensePlateSpottedEvent>(actualDomainEvent);
      var actualSpotFromEvent = Assert.Single(actualLicensePlateSpotAddedEvent.LicensePlateSpotModels);
      Assert.Equal(lpSpot, actualSpotFromEvent);
    }

    [Fact]
    public void CanRemoveSpottedPlates()
    {
      var existingSpottedBy = new MockPlayerModel(null, 2, "existing");
      var toRemove = (Country.US, StateOrProvince.CA);

      var existingSpot = new MockLicensePlateSpotModel(
        new MockLicensePlateModel(Country.US, StateOrProvince.CA),
        existingSpottedBy);

      var uut = new MockGameModel(new[] { existingSpot }, null, true, null);

      var actual = uut.RemoveLicensePlateSpot(new[] { toRemove },
        existingSpottedBy);

      Assert.True(actual.IsSuccess);
      Assert.Empty(uut.LicensePlateSpots);
      var actualEvent = Assert.Single(uut.DomainEvents);
      var actualRemovedEvent = Assert.IsType<LicensePlateSpotRemovedEvent>(actualEvent);
      var removedPlate = Assert.Single(actualRemovedEvent.LicensePlatesToRemove);
      Assert.Equal((existingSpot.LicensePlate.Country, existingSpot.LicensePlate.StateOrProvince),
        removedPlate);
    }

    [Fact]
    public void CanHandleAlreadySpottedPlate()
    {
      var existingSpottedBy = new MockPlayerModel(null, 2, "existing");
      var spottedBy = new MockPlayerModel(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var existingSpot = new MockLicensePlateSpotModel(
        new MockLicensePlateModel(Country.US, StateOrProvince.CA),
        existingSpottedBy);

      var newSpot = new MockLicensePlateSpotModel(
        new MockLicensePlateModel(Country.US, StateOrProvince.CA),
        spottedBy);

      var lpFactory = new Mock<ILicensePlateSpotFactory>();
      lpFactory
        .Setup(fac => fac.SpotLicensePlate(Country.US, StateOrProvince.CA, spottedBy))
        .Returns(Result.Success<LicensePlateSpot>(newSpot));

      var uut = new MockGameModel(new [] { existingSpot }, null, true, null);

      var actual = uut.AddLicensePlateSpot(lpFactory.Object,
        new[] { toSpot },
        spottedBy);

      Assert.True(actual.IsSuccess);
      var actualLicensePlateSpot = Assert.Single(uut.LicensePlateSpots);
      Assert.Equal(existingSpot, actualLicensePlateSpot);
      Assert.Equal(existingSpottedBy, actualLicensePlateSpot.SpottedBy);
      Assert.Empty(uut.DomainEvents);
    }

    [Fact]
    public void CanValidateGameStatus()
    {
      var spottedBy = new MockPlayerModel(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var lpSpot = new MockLicensePlateSpotModel(
        new MockLicensePlateModel(Country.US, StateOrProvince.CA),
        spottedBy);

      var lpFactory = new Mock<ILicensePlateSpotFactory>();
      lpFactory
        .Setup(fac => fac.SpotLicensePlate(Country.US, StateOrProvince.CA, spottedBy))
        .Returns(Result.Success<LicensePlateSpot>(lpSpot));

      var uut = new MockGameModel(null, null, false, null);

      var actual = uut.AddLicensePlateSpot(lpFactory.Object,
        new[] { toSpot },
        spottedBy);

      Assert.False(actual.IsSuccess);
      Assert.Equal(Game.InactiveGameError, actual.ErrorMessage);
      Assert.Empty(uut.LicensePlateSpots);
      Assert.Empty(uut.DomainEvents);
    }
  }
}
