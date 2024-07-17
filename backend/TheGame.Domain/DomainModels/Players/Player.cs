using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.DomainModels.Players;

public partial class Player : RootModel
{
  public virtual ICollection<Game> InvitedGames { get; private set; } = [];

  protected HashSet<GamePlayer> _invitedGamePlayers = [];
  public virtual ICollection<GamePlayer> InvatedGamePlayers => _invitedGamePlayers;

  public long Id { get; protected set; }

  public string Name { get; protected set; } = default!;

  public long PlayerIdentityId { get; protected set; }
  public virtual PlayerIdentity? PlayerIdentity { get; protected set; }
}
