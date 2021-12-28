using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public partial class LicensePlateSpot
  {
    public class LicensePlateSpotFactory : ILicensePlateSpotFactory
    {
      public LicensePlateSpotFactory()
      {
      }

      public Result<LicensePlateSpot> SpotLicensePlate(Country country,
        StateOrProvince stateOrProvince,
        Player spottedBy)
      {
        var licensePlateResult = LicensePlate.GetLicensePlate(country, stateOrProvince);
        if (!licensePlateResult.IsSuccess)
        {
          return Result.Error<LicensePlateSpot>(licensePlateResult.ErrorMessage);
        }

        var newSpot = new LicensePlateSpot
        {
          LicensePlate = licensePlateResult.Value,
          SpottedBy = spottedBy
        };
        return Result.Success(newSpot);
      }
    }
  }
}
