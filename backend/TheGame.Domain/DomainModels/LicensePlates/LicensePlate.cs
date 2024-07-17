using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.LicensePlates;

public class LicensePlate : IEnumeration, IEquatable<LicensePlate>
{
  public sealed record PlateKey(Country Country, StateOrProvince StateOrProvince);

  public static class ErrorMessages
  {
    public const string LicensePlateNotFoundError = "license_plate_not_found";
  }

  public static readonly IReadOnlyCollection<LicensePlate> AvailableLicensePlates =
  [
    new LicensePlate { Id = 1, StateOrProvince = StateOrProvince.AL, Country = Country.US },
    new LicensePlate { Id = 2, StateOrProvince = StateOrProvince.AK, Country = Country.US },
    new LicensePlate { Id = 3, StateOrProvince = StateOrProvince.AZ, Country = Country.US },
    new LicensePlate { Id = 4, StateOrProvince = StateOrProvince.AR, Country = Country.US },
    new LicensePlate { Id = 5, StateOrProvince = StateOrProvince.CA, Country = Country.US },
    new LicensePlate { Id = 6, StateOrProvince = StateOrProvince.CO, Country = Country.US },
    new LicensePlate { Id = 7, StateOrProvince = StateOrProvince.CT, Country = Country.US },
    new LicensePlate { Id = 8, StateOrProvince = StateOrProvince.DE, Country = Country.US },
    new LicensePlate { Id = 9, StateOrProvince = StateOrProvince.DC, Country = Country.US },
    new LicensePlate { Id = 10, StateOrProvince = StateOrProvince.FL, Country = Country.US },
    new LicensePlate { Id = 11, StateOrProvince = StateOrProvince.GA, Country = Country.US },
    new LicensePlate { Id = 12, StateOrProvince = StateOrProvince.HI, Country = Country.US },
    new LicensePlate { Id = 13, StateOrProvince = StateOrProvince.ID, Country = Country.US },
    new LicensePlate { Id = 14, StateOrProvince = StateOrProvince.IL, Country = Country.US },
    new LicensePlate { Id = 15, StateOrProvince = StateOrProvince.IN, Country = Country.US },
    new LicensePlate { Id = 16, StateOrProvince = StateOrProvince.IA, Country = Country.US },
    new LicensePlate { Id = 17, StateOrProvince = StateOrProvince.KS, Country = Country.US },
    new LicensePlate { Id = 18, StateOrProvince = StateOrProvince.KY, Country = Country.US },
    new LicensePlate { Id = 19, StateOrProvince = StateOrProvince.LA, Country = Country.US },
    new LicensePlate { Id = 20, StateOrProvince = StateOrProvince.ME, Country = Country.US },
    new LicensePlate { Id = 21, StateOrProvince = StateOrProvince.MD, Country = Country.US },
    new LicensePlate { Id = 22, StateOrProvince = StateOrProvince.MA, Country = Country.US },
    new LicensePlate { Id = 23, StateOrProvince = StateOrProvince.MI, Country = Country.US },
    new LicensePlate { Id = 24, StateOrProvince = StateOrProvince.MN, Country = Country.US },
    new LicensePlate { Id = 25, StateOrProvince = StateOrProvince.MS, Country = Country.US },
    new LicensePlate { Id = 26, StateOrProvince = StateOrProvince.MO, Country = Country.US },
    new LicensePlate { Id = 27, StateOrProvince = StateOrProvince.MT, Country = Country.US },
    new LicensePlate { Id = 28, StateOrProvince = StateOrProvince.NE, Country = Country.US },
    new LicensePlate { Id = 29, StateOrProvince = StateOrProvince.NV, Country = Country.US },
    new LicensePlate { Id = 30, StateOrProvince = StateOrProvince.NH, Country = Country.US },
    new LicensePlate { Id = 31, StateOrProvince = StateOrProvince.NJ, Country = Country.US },
    new LicensePlate { Id = 32, StateOrProvince = StateOrProvince.NM, Country = Country.US },
    new LicensePlate { Id = 33, StateOrProvince = StateOrProvince.NY, Country = Country.US },
    new LicensePlate { Id = 34, StateOrProvince = StateOrProvince.NC, Country = Country.US },
    new LicensePlate { Id = 35, StateOrProvince = StateOrProvince.ND, Country = Country.US },
    new LicensePlate { Id = 36, StateOrProvince = StateOrProvince.OH, Country = Country.US },
    new LicensePlate { Id = 37, StateOrProvince = StateOrProvince.OK, Country = Country.US },
    new LicensePlate { Id = 38, StateOrProvince = StateOrProvince.OR, Country = Country.US },
    new LicensePlate { Id = 39, StateOrProvince = StateOrProvince.PA, Country = Country.US },
    new LicensePlate { Id = 40, StateOrProvince = StateOrProvince.RI, Country = Country.US },
    new LicensePlate { Id = 41, StateOrProvince = StateOrProvince.SC, Country = Country.US },
    new LicensePlate { Id = 42, StateOrProvince = StateOrProvince.SD, Country = Country.US },
    new LicensePlate { Id = 43, StateOrProvince = StateOrProvince.TN, Country = Country.US },
    new LicensePlate { Id = 44, StateOrProvince = StateOrProvince.TX, Country = Country.US },
    new LicensePlate { Id = 45, StateOrProvince = StateOrProvince.UT, Country = Country.US },
    new LicensePlate { Id = 46, StateOrProvince = StateOrProvince.VT, Country = Country.US },
    new LicensePlate { Id = 47, StateOrProvince = StateOrProvince.VA, Country = Country.US },
    new LicensePlate { Id = 48, StateOrProvince = StateOrProvince.WA, Country = Country.US },
    new LicensePlate { Id = 49, StateOrProvince = StateOrProvince.WV, Country = Country.US },
    new LicensePlate { Id = 50, StateOrProvince = StateOrProvince.WI, Country = Country.US },
    new LicensePlate { Id = 51, StateOrProvince = StateOrProvince.WY, Country = Country.US },
    // canada
    new LicensePlate { Id = 52, StateOrProvince = StateOrProvince.AB,  Country = Country.CA },
    new LicensePlate { Id = 53, StateOrProvince = StateOrProvince.BC,  Country = Country.CA },
    new LicensePlate { Id = 54, StateOrProvince = StateOrProvince.MB,  Country = Country.CA },
    new LicensePlate { Id = 55, StateOrProvince = StateOrProvince.NB,  Country = Country.CA },
    new LicensePlate { Id = 56, StateOrProvince = StateOrProvince.NL,  Country = Country.CA },
    new LicensePlate { Id = 57, StateOrProvince = StateOrProvince.NT,  Country = Country.CA },
    new LicensePlate { Id = 58, StateOrProvince = StateOrProvince.NS,  Country = Country.CA },
    new LicensePlate { Id = 59, StateOrProvince = StateOrProvince.NU,  Country = Country.CA },
    new LicensePlate { Id = 60, StateOrProvince = StateOrProvince.ON,  Country = Country.CA },
    new LicensePlate { Id = 61, StateOrProvince = StateOrProvince.PE,  Country = Country.CA },
    new LicensePlate { Id = 62, StateOrProvince = StateOrProvince.QC,  Country = Country.CA },
    new LicensePlate { Id = 63, StateOrProvince = StateOrProvince.SK,  Country = Country.CA },
    new LicensePlate { Id = 64, StateOrProvince = StateOrProvince.YT,  Country = Country.CA }
  ];

  public static readonly ReadOnlyDictionary<PlateKey, LicensePlate> LicensePlatesByCountryAndProvinceLookup =
    AvailableLicensePlates
      .ToDictionary(lp => new PlateKey(lp.Country, lp.StateOrProvince))
      .AsReadOnly();

  public long Id { get; protected set; }

  public StateOrProvince StateOrProvince { get; protected set; }

  public Country Country { get; protected set; }

  public static OneOf<LicensePlate, Failure> GetLicensePlate(PlateKey plateKey)
  {
    if (LicensePlatesByCountryAndProvinceLookup.TryGetValue(plateKey, out var licensePlateModel))
    {
      return licensePlateModel;
    }

    return new Failure(ErrorMessages.LicensePlateNotFoundError);
  }

  public override string ToString() => $"{Id}_{Country}_{StateOrProvince}";

  public override int GetHashCode() => ToString().GetHashCode();

  public bool Equals(LicensePlate? other)
  {
    if (other is null)
    {
      return false;
    }

    return Country == other.Country &&
      StateOrProvince == other.StateOrProvince &&
      Id == other.Id;
  }

  public override bool Equals(object? obj)
  {
    return Equals(obj as LicensePlate);
  }
}

public enum StateOrProvince
{
  // US
  AL,
  AK,
  AZ,
  AR,
  CA,
  CO,
  CT,
  DE,
  DC,
  FL,
  GA,
  HI,
  ID,
  IL,
  IN,
  IA,
  KS,
  KY,
  LA,
  ME,
  MD,
  MA,
  MI,
  MN,
  MS,
  MO,
  MT,
  NE,
  NV,
  NH,
  NJ,
  NM,
  NY,
  NC,
  ND,
  OH,
  OK,
  OR,
  PA,
  RI,
  SC,
  SD,
  TN,
  TX,
  UT,
  VT,
  VA,
  WA,
  WV,
  WI,
  WY,
  // Canada
  AB,
  BC,
  MB,
  NB,
  NL,
  NT,
  NS,
  NU,
  ON,
  PE,
  QC,
  SK,
  YT,
}

public enum Country
{
  US,
  CA
}
