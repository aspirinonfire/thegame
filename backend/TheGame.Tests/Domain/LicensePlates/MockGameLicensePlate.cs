using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Tests.Domain.Players;

namespace TheGame.Tests.Domain.LicensePlates
{
  internal class MockGameLicensePlate : GameLicensePlate
  {
    public MockGameLicensePlate(MockLicensePlate licensePlateModel,
      MockPlayer spottedBy)
    {
      LicensePlate = licensePlateModel;
      SpottedBy = spottedBy;
    }
  }
}
