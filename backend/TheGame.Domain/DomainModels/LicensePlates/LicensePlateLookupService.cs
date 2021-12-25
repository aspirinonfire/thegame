using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public class LicensePlateLookupService : ILicensePlateLookupService
  {
    private readonly IReadOnlyDictionary<(Country country, StateOrProvince stateOrProvince), LicensePlateModel> _lookup;

    private LicensePlateLookupService(IReadOnlyDictionary<(Country country, StateOrProvince stateOrProvince), LicensePlateModel> lookup)
    {
      _lookup = lookup;
    }

    public LicensePlateModel GetPlateByCountryAndState(Country country, StateOrProvince stateOrProvince)
    {
      if (_lookup.TryGetValue((country, stateOrProvince), out var licensePlate))
      {
        return licensePlate;
      }
      return null;
    }

    public static LicensePlateLookupService CreateLookupService()
    {
      // TODO implement once DB context is ready
      var lpMap = new Dictionary<(Country country, StateOrProvince stateOrProvince), LicensePlateModel>();
      return new LicensePlateLookupService(lpMap);
    }
  }
}
