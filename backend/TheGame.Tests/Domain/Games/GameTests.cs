using Moq;
using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.Utils;
using TheGame.Tests.Domain.LicensePlates;
using TheGame.Tests.Domain.Players;
using TheGame.Tests.TestUtils;
using Xunit;

namespace TheGame.Tests.Domain.Games
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class GameTests
  {


    [Fact]
    public void CanSpotPlate()
    {
      var spottedBy = new MockPlayer(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var lpSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var lpFactory = new Mock<IGameLicensePlateFactory>();
      lpFactory
        .Setup(fac => fac.CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultDate))
        .Returns(Result.Success<GameLicensePlate>(lpSpot));

      var uut = new MockGame(null, null, true, null);

      var actual = uut.AddLicensePlateSpot(lpFactory.Object,
        CommonMockedServices.GetSystemService(),
        new[] { toSpot },
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
    public void CanRemoveSpottedPlates()
    {
      var existingSpottedBy = new MockPlayer(null, 2, "existing");
      var toRemove = (Country.US, StateOrProvince.CA);

      var existingSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        existingSpottedBy);

      var uut = new MockGame(new[] { existingSpot }, null, true, null);

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
    public void CanHandleAlreadySpottedPlate()
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

      var lpFactory = new Mock<IGameLicensePlateFactory>();
      lpFactory
        .Setup(fac => fac.CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultDate))
        .Returns(Result.Success<GameLicensePlate>(newSpot));

      var uut = new MockGame(new [] { existingSpot }, null, true, null);

      var actual = uut.AddLicensePlateSpot(lpFactory.Object,
        CommonMockedServices.GetSystemService(),
        new[] { toSpot },
        spottedBy);

      Assert.True(actual.IsSuccess);
      var actualLicensePlateSpot = Assert.Single(uut.GameLicensePlates);
      Assert.Equal(existingSpot, actualLicensePlateSpot);
      Assert.Equal(existingSpottedBy, actualLicensePlateSpot.SpottedBy);
      Assert.Empty(uut.DomainEvents);
    }

    [Fact]
    public void CanValidateGameStatus()
    {
      var spottedBy = new MockPlayer(null, 1, "test");
      var toSpot = (Country.US, StateOrProvince.CA);

      var lpSpot = new MockGameLicensePlate(
        new MockLicensePlate(Country.US, StateOrProvince.CA),
        spottedBy);

      var lpFactory = new Mock<IGameLicensePlateFactory>();
      lpFactory
        .Setup(fac => fac.CreateLicensePlateSpot(Country.US, StateOrProvince.CA, spottedBy, CommonMockedServices.DefaultDate))
        .Returns(Result.Success<GameLicensePlate>(lpSpot));

      var uut = new MockGame(null, null, false, null);

      var actual = uut.AddLicensePlateSpot(lpFactory.Object,
        CommonMockedServices.GetSystemService(),
        new[] { toSpot },
        spottedBy);

      Assert.False(actual.IsSuccess);
      Assert.Equal(Game.InactiveGameError, actual.ErrorMessage);
      Assert.Empty(uut.GameLicensePlates);
      Assert.Empty(uut.DomainEvents);
    }
  }
}
