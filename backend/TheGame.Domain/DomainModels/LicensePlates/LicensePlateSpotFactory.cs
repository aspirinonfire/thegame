using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public partial class LicensePlateSpotModel
  {
    public const string LicensePlateNotFoundError = "license_plate_not_found";

    public class LicensePlateSpotFactory : ILicensePlateSpotFactory
    {
      private readonly ILicensePlateLookupService _licensePlateLookupSvc;

      public LicensePlateSpotFactory(ILicensePlateLookupService licensePlateLookupSvc)
      {
        _licensePlateLookupSvc = licensePlateLookupSvc;
      }

      public Result<LicensePlateSpotModel> SpotLicensePlate(Country country,
        StateOrProvince stateOrProvince,
        PlayerModel spottedBy)
      {
        var licensePlate = _licensePlateLookupSvc.GetPlateByCountryAndState(country, stateOrProvince);
        if (licensePlate == null)
        {
          return Result.Error<LicensePlateSpotModel>(LicensePlateNotFoundError);
        }

        var newSpot = new LicensePlateSpotModel
        {
          LicensePlate = licensePlate,
          SpottedBy = spottedBy
        };
        return Result.Success(newSpot);
      }
    }
  }
}
