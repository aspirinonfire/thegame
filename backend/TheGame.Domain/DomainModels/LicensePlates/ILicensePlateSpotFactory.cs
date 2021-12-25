using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public interface ILicensePlateSpotFactory
  {
    Result<LicensePlateSpotModel> SpotLicensePlate(Country country,
      StateOrProvince stateOrProvince,
      PlayerModel spottedBy);
  }
}
