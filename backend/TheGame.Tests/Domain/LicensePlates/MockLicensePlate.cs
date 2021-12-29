using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Tests.Domain.LicensePlates
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
