using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.LicensePlate;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Tests.Domain.Game
{
  public class MockGameModel : GameModel
  {
    public MockGameModel(TeamModel? team,
      IEnumerable<LicensePlateModel>? licensePlates,
      string? name,
      bool isActive,
      DateTimeOffset? endedOn)
    {
      _team = team;

      _licensePlates = licensePlates != null ?
        new HashSet<LicensePlateModel>(licensePlates) :
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
