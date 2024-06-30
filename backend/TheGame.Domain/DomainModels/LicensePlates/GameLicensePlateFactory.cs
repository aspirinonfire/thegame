using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates;

public interface IGameLicensePlateFactory
{
  DomainResult<GameLicensePlate> CreateLicensePlateSpot(Country country,
    StateOrProvince stateOrProvince,
    Player spottedBy,
    DateTimeOffset spotDate);
}

public partial class GameLicensePlate
{
  public class LicensePlateSpotFactory : IGameLicensePlateFactory
  {
    public LicensePlateSpotFactory()
    { }

    public DomainResult<GameLicensePlate> CreateLicensePlateSpot(Country country,
      StateOrProvince stateOrProvince,
      Player spottedBy,
      DateTimeOffset spotDate)
    {
      var licensePlateResult = LicensePlate.GetLicensePlate(country, stateOrProvince);
      if (!licensePlateResult.IsSuccess || licensePlateResult.HasNoValue)
      {
        return DomainResult.Error<GameLicensePlate>(licensePlateResult.ErrorMessage ?? LicensePlate.ErrorMessages.LicensePlateNotFoundError);
      }

      var newSpot = new GameLicensePlate
      {
        LicensePlate = licensePlateResult.Value!,
        SpottedBy = spottedBy,
        DateCreated = spotDate
      };
      return DomainResult.Success(newSpot);
    }
  }
}
