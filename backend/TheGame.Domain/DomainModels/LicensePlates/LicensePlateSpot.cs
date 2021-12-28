using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  public partial class LicensePlateSpot : BaseModel, IEquatable<LicensePlateSpot>
  {
    public LicensePlate LicensePlate { get; protected set; }
    public Player SpottedBy { get; protected set; }

    public override int GetHashCode() => LicensePlate.GetHashCode();

    public override bool Equals(object obj) => Equals(obj as LicensePlateSpot);

    public bool Equals(LicensePlateSpot other)
    {
      if (other == null)
      {
        return false;
      }

      return LicensePlate == other.LicensePlate;
    }

    public static bool operator ==(LicensePlateSpot lhs, LicensePlateSpot rhs)
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

    public static bool operator !=(LicensePlateSpot lhs, LicensePlateSpot rhs) => !(lhs == rhs);
  }
}
