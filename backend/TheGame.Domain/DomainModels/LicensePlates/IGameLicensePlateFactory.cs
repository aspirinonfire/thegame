using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public interface IGameLicensePlateFactory
  {
    DomainResult<GameLicensePlate> CreateLicensePlateSpot(Country country,
      StateOrProvince stateOrProvince,
      Player spottedBy,
      DateTimeOffset spotDate);
  }
}
