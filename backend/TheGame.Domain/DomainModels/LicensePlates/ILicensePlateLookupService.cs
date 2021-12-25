namespace TheGame.Domain.DomainModels.LicensePlates
{
  public interface ILicensePlateLookupService
  {
    LicensePlateModel GetPlateByCountryAndState(Country country, StateOrProvince stateOrProvince);
  }
}