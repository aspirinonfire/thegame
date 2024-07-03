using System;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates;

public interface IGameLicensePlateFactory
{
  OneOf<GameLicensePlate, Failure> CreateLicensePlateSpot(Country country,
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

    public OneOf<GameLicensePlate, Failure> CreateLicensePlateSpot(Country country,
      StateOrProvince stateOrProvince,
      Player spottedBy,
      DateTimeOffset spotDate)
    {
      var licensePlateResult = LicensePlate.GetLicensePlate(country, stateOrProvince);
      if (!licensePlateResult.TryGetSuccessful(out var success, out var failure))
      {
        return failure;
      }

      var newSpot = new GameLicensePlate
      {
        LicensePlate = success,
        SpottedBy = spottedBy,
        DateCreated = spotDate
      };

      return newSpot;
    }
  }
}