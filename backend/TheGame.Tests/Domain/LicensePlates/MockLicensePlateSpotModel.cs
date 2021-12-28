using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Tests.Domain.Players;

namespace TheGame.Tests.Domain.LicensePlates
{
  internal class MockLicensePlateSpotModel : LicensePlateSpot
  {
    public MockLicensePlateSpotModel(MockLicensePlateModel licensePlateModel,
      MockPlayerModel spottedBy)
    {
      LicensePlate = licensePlateModel;
      SpottedBy = spottedBy;
    }
  }
}
