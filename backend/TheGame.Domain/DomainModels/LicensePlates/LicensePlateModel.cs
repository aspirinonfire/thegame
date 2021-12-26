using System;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public class LicensePlateModel : BaseModel, IEquatable<LicensePlateModel>
  {
    public long Id { get; }

    public StateOrProvince StateOrProvince { get; protected set; }

    public Country Country { get; protected set; }


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
    CA,

    // Canada
    BritishColumbia
  }

  public enum Country
  {
    US,
    CA,
    MX
  }
}
