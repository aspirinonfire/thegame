using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Tests.Domain.Players;

namespace TheGame.Tests.Domain.LicensePlates
{
  internal class MockLicensePlateSpotModel : LicensePlateSpotModel
  {
    public MockLicensePlateSpotModel(MockLicensePlateModel licensePlateModel,
      MockPlayerModel spottedBy)
    {
      LicensePlate = licensePlateModel;
      SpottedBy = spottedBy;
    }
  }
}
