using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Tests.DomainModels.LicensePlates
{
  internal class MockLicensePlate : LicensePlate
  {
    public MockLicensePlate(PlateKey plateKey)
    {
      Country = plateKey.Country;
      StateOrProvince = plateKey.StateOrProvince;
    }
  }
}
