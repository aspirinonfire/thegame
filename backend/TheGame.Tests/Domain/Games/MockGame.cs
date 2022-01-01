using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Tests.Domain.Games
{
  public class MockGame : Game
  {
    public MockGame(IEnumerable<GameLicensePlate>? licensePlates,
      bool isActive,
      DateTimeOffset? endedOn,
      string name = "test game")
    {
      _gameLicensePlates = licensePlates != null ?
        new HashSet<GameLicensePlate>(licensePlates) :
        new ();

      Name = name;

      IsActive = isActive;

      EndedOn = endedOn;
    }

    public void SetActiveFlag(bool newValue)
    {
      IsActive = newValue;
    }
  }
}
