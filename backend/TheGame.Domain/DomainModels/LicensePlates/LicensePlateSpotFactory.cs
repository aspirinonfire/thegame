using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public partial class LicensePlateSpotModel
  {
    public class LicensePlateSpotFactory : ILicensePlateSpotFactory
    {
      public LicensePlateSpotFactory()
      {
      }

      public Result<LicensePlateSpotModel> SpotLicensePlate(Country country,
        StateOrProvince stateOrProvince,
        PlayerModel spottedBy)
      {
        var licensePlateResult = LicensePlateModel.GetLicensePlate(country, stateOrProvince);
        if (!licensePlateResult.IsSuccess)
        {
          return Result.Error<LicensePlateSpotModel>(licensePlateResult.ErrorMessage);
        }

        var newSpot = new LicensePlateSpotModel
        {
          LicensePlate = licensePlateResult.Value,
          SpottedBy = spottedBy
        };
        return Result.Success(newSpot);
      }
    }
  }
}
