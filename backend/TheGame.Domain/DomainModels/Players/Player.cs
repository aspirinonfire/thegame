using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Teams;

namespace TheGame.Domain.DomainModels.Players
{
  public partial class Player : BaseModel, IEquatable<Player>
  {
    protected HashSet<Team> _teams = new();

    public long UserId { get; protected set; }
    public string Name { get; protected set; }

    public override int GetHashCode() => UserId.GetHashCode();

    public override bool Equals(object obj) => Equals(obj as Player);

    public bool Equals(Player other)
    {
      if (other == null)
      {
        return false;
      }

      return UserId == other.UserId;
    }

    public static bool operator ==(Player lhs, Player rhs)
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

    public static bool operator !=(Player lhs, Player rhs) => !(lhs == rhs);
  }
}