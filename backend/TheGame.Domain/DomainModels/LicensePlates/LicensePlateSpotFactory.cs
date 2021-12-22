using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public partial class LicensePlateSpotModel
  {
    public class LicensePlateSpotFactory : ILicensePlateSpotFactory
    {
      public Result<LicensePlateSpotModel> SpotLicensePlate(LicensePlateModel licensePlate,
        PlayerModel spottedBy)
      {
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
