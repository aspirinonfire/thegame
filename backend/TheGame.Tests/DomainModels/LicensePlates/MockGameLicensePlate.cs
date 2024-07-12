using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Tests.DomainModels.LicensePlates
{
  internal class MockGameLicensePlate : GameLicensePlate
  {
    public MockGameLicensePlate(LicensePlate licensePlateModel,
      Player spottedBy,
      DateTimeOffset? spottedOn = null)
    {
      LicensePlate = licensePlateModel;
      SpottedBy = spottedBy;
      DateCreated = spottedOn.GetValueOrDefault(CommonMockedServices.DefaultTestDate);
    }
  }
}
