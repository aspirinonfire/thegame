using System;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates;

public interface IGameLicensePlateFactory
{
  Maybe<GameLicensePlate> CreateLicensePlateSpot(LicensePlate.PlateKey plateKey,
    Player spottedBy,
    DateTimeOffset spotDate);
}

public partial class GameLicensePlate
{
  public class LicensePlateSpotFactory() : IGameLicensePlateFactory
  {
    public Maybe<GameLicensePlate> CreateLicensePlateSpot(LicensePlate.PlateKey plateKey,
      Player spottedBy,
      DateTimeOffset spotDate)
    {
      var licensePlateResult = LicensePlate.GetLicensePlate(plateKey);
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
