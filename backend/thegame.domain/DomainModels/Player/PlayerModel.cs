using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Domain.DomainModels.Player
{
  public partial class PlayerModel : BaseModel, IEquatable<PlayerModel>
  {
    protected HashSet<TeamModel> _teams = new();

    public long UserId { get; protected set; }
    public string Name { get; protected set; }

    public override int GetHashCode() => UserId.GetHashCode();

    public override bool Equals(object obj) => Equals(obj as PlayerModel);

    public bool Equals(PlayerModel other)
    {
      if (other == null)
      {
        return false;
      }

      return UserId == other.UserId;
    }

    public static bool operator ==(PlayerModel lhs, PlayerModel rhs)
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

    public static bool operator !=(PlayerModel lhs, PlayerModel rhs) => !(lhs == rhs);
  }
}
