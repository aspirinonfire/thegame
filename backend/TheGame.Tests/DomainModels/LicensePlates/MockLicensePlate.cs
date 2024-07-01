using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Tests.DomainModels.LicensePlates
{
  internal class MockLicensePlate : LicensePlate
  {
    public MockLicensePlate(Country country,
      StateOrProvince stateOrProvince)
    {
      Country = country;
      StateOrProvince = stateOrProvince;
    }
  }
}
