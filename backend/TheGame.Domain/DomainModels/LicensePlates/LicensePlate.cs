using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;

namespace TheGame.Domain.DomainModels.LicensePlates;

public class LicensePlate : BaseModel, IEquatable<LicensePlate>
{
  public static class ErrorMessages
  {
    public const string LicensePlateNotFoundError = "license_plate_not_found";
  }

  public static readonly IReadOnlyCollection<LicensePlate> AvailableLicensePlates = new List<LicensePlate>()
  {
    // TODO populate the rest
    new LicensePlate() { Id = 1, Country = Country.CA, StateOrProvince = StateOrProvince.BC },
    new LicensePlate() { Id = 2, Country = Country.US, StateOrProvince = StateOrProvince.AK },
    new LicensePlate() { Id = 3, Country = Country.US, StateOrProvince = StateOrProvince.CA },
    new LicensePlate() { Id = 4, Country = Country.US, StateOrProvince = StateOrProvince.NV },
    new LicensePlate() { Id = 5, Country = Country.US, StateOrProvince = StateOrProvince.OR },
    new LicensePlate() { Id = 6, Country = Country.US, StateOrProvince = StateOrProvince.WA },
  };

  private static readonly ReadOnlyDictionary<(Country, StateOrProvince), LicensePlate> _licensePlateMap =
    AvailableLicensePlates
      .ToDictionary(lp => (lp.Country, lp.StateOrProvince))
      .AsReadOnly();

  public long Id { get; protected set; }

  public StateOrProvince StateOrProvince { get; protected set; }

  public Country Country { get; protected set; }

  public virtual ICollection<Game> Games { get; protected set; } = default!;

  protected HashSet<GameLicensePlate> _gameLicensePlates = [];
  public virtual ICollection<GameLicensePlate> GameLicensePlates => _gameLicensePlates;

  public LicensePlate() { }

  public static DomainResult<LicensePlate> GetLicensePlate(Country country, StateOrProvince stateOrProvince)
  {
    if (_licensePlateMap.TryGetValue((country, stateOrProvince), out var licensePlateModel))
    {
      return DomainResult.Success(licensePlateModel);
    }
    return DomainResult.Error<LicensePlate>(ErrorMessages.LicensePlateNotFoundError);
  }

  public override string ToString() => $"{Id}_{Country}_{StateOrProvince}";

  public override int GetHashCode() => $"{Country}_{StateOrProvince}".GetHashCode();

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
  public override bool Equals(object obj) => Equals(obj as LicensePlate);
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

  public bool Equals(LicensePlate? other)
  {
    if (other is null)
    {
      return false;
    }

    return Country == other.Country &&
      StateOrProvince == other.StateOrProvince;
  }

  public static bool operator ==(LicensePlate lhs, LicensePlate rhs)
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

  public static bool operator !=(LicensePlate lhs, LicensePlate rhs) => !(lhs == rhs);
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

  // Mexico
}

public enum Country
{
  US,
  CA,
  MX
}
