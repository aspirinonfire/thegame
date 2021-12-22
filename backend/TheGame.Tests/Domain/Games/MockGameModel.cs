using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Tests.Domain.Games
{
  public class MockGameModel : GameModel
  {
    public MockGameModel(TeamModel? team,
      IEnumerable<LicensePlateSpotModel>? licensePlates,
      string? name,
      bool isActive,
      DateTimeOffset? endedOn)
    {
      _team = team;

      _licensePlates = licensePlates != null ?
        new HashSet<LicensePlateSpotModel>(licensePlates) :
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
