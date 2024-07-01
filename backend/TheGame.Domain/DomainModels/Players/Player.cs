using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.DomainModels.Players;

public partial class Player : BaseModel, IEquatable<Player>
{
  public virtual ICollection<Game> InvitedGames { get; private set; } = [];

  protected HashSet<GamePlayer> _invitedGamePlayers = [];
  public virtual ICollection<GamePlayer> InvatedGamePlayers => _invitedGamePlayers;

  public long Id { get; protected set; }

  public string Name { get; protected set; } = default!;

  public long PlayerIdentityId { get; protected set; }
  public virtual PlayerIdentity? PlayerIdentity { get; protected set; }

  public override int GetHashCode() => Id.GetHashCode();

  public override bool Equals(object? obj) => Equals(obj as Player);

  public bool Equals(Player? other) => Id == other?.Id;

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
