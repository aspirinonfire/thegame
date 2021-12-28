using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Tests.Domain.Games
{
  public class MockGameModel : Game
  {
    public MockGameModel(IEnumerable<LicensePlateSpot>? licensePlates,
      string? name,
      bool isActive,
      DateTimeOffset? endedOn)
    {
      _licensePlateSpots = licensePlates != null ?
        new HashSet<LicensePlateSpot>(licensePlates) :
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
