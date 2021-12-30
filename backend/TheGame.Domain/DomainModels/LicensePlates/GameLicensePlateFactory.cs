using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public partial class GameLicensePlate
  {
    public class LicensePlateSpotFactory : IGameLicensePlateFactory
    {
      public LicensePlateSpotFactory()
      { }

      public Result<GameLicensePlate> CreateLicensePlateSpot(Country country,
        StateOrProvince stateOrProvince,
        Player spottedBy,
        DateTimeOffset spotDate)
      {
        var licensePlateResult = LicensePlate.GetLicensePlate(country, stateOrProvince);
        if (!licensePlateResult.IsSuccess)
        {
          return Result.Error<GameLicensePlate>(licensePlateResult.ErrorMessage ?? LicensePlate.ErrorMessages.LicensePlateNotFoundError);
        }

        var newSpot = new GameLicensePlate
        {
          LicensePlate = licensePlateResult.Value,
          SpottedBy = spottedBy,
          DateCreated = spotDate
        };
        return Result.Success(newSpot);
      }
    }
  }
}
