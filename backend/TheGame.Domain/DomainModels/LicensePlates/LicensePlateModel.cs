using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public class LicensePlateModel : BaseModel, IEquatable<LicensePlateModel>
  {
    public const string LicensePlateNotFoundError = "license_plate_not_found";

    public static readonly IReadOnlyCollection<LicensePlateModel> AvailableLicensePlates = new List<LicensePlateModel>()
    {
      // TODO populate the rest
      new LicensePlateModel() { Id = 1, Country = Country.CA, StateOrProvince = StateOrProvince.BC },
      new LicensePlateModel() { Id = 2, Country = Country.US, StateOrProvince = StateOrProvince.AK },
      new LicensePlateModel() { Id = 3, Country = Country.US, StateOrProvince = StateOrProvince.CA },
      new LicensePlateModel() { Id = 4, Country = Country.US, StateOrProvince = StateOrProvince.NV },
      new LicensePlateModel() { Id = 5, Country = Country.US, StateOrProvince = StateOrProvince.OR },
      new LicensePlateModel() { Id = 6, Country = Country.US, StateOrProvince = StateOrProvince.WA },
    };

    private static readonly IReadOnlyDictionary<(Country, StateOrProvince), LicensePlateModel> _licensePlateMap =
      AvailableLicensePlates
        .ToDictionary(lp => (lp.Country, lp.StateOrProvince));

    public long Id { get; protected set; }

    public StateOrProvince StateOrProvince { get; protected set; }

    public Country Country { get; protected set; }

    public static Result<LicensePlateModel> GetLicensePlate(Country country, StateOrProvince stateOrProvince)
    {
      if (_licensePlateMap.TryGetValue((country, stateOrProvince), out var licensePlateModel))
      {
        return Result.Success(licensePlateModel);
      }
      return Result.Error<LicensePlateModel>(LicensePlateNotFoundError);
    }

    public override string ToString() => $"{Id}_{Country}_{StateOrProvince}";

    public override int GetHashCode() => $"{Country}_{StateOrProvince}".GetHashCode();

    public override bool Equals(object obj) => Equals(obj as LicensePlateModel);

    public bool Equals(LicensePlateModel other)
    {
      if (other == null)
      {
        return false;
      }

      return Country == other.Country &&
        StateOrProvince == other.StateOrProvince;
    }

    public static bool operator ==(LicensePlateModel lhs, LicensePlateModel rhs)
    {
      if (lhs is null)
      {
        if (rhs is null)
        {
          return true;
        }

        // Only the left side is null.
        return false;
      }
      // Equals handles case of null on right side.
      return lhs.Equals(rhs);
    }

    public static bool operator !=(LicensePlateModel lhs, LicensePlateModel rhs) => !(lhs == rhs);
  }

  //TODO Populate
  public enum StateOrProvince
  {
    // US
    AK,
    CA,
    NV,
    OR,
    WA,

    // Canada
    BC
  }

  public enum Country
  {
    US,
    CA,
    MX
  }
}
