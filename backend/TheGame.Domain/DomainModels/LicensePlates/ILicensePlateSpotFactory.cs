using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public interface ILicensePlateSpotFactory
  {
    Result<LicensePlateSpotModel> SpotLicensePlate(LicensePlateModel licensePlate, PlayerModel spottedBy);
  }
}