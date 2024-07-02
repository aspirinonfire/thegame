using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Tests.DomainModels.Players;

namespace TheGame.Tests.DomainModels.LicensePlates
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
