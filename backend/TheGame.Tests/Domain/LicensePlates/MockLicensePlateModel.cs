using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Tests.Domain.LicensePlates
{
  internal class MockLicensePlateModel : LicensePlate
  {
    public MockLicensePlateModel(Country country,
      StateOrProvince stateOrProvince)
    {
      Country = country;
      StateOrProvince = stateOrProvince;
    }
  }
}
