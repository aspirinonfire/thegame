using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.DomainModels.Players;

public partial class Player : RootModel
{
  public IReadOnlySet<Game> InvitedGames { get; protected set; } = default!;

  protected HashSet<GamePlayer> _invitedGamePlayers = [];
  public IReadOnlySet<GamePlayer> InvatedGamePlayers => _invitedGamePlayers;

  protected HashSet<Game> _ownedGames = [];
  public IReadOnlySet<Game> OwnedGames => _ownedGames;


  public long Id { get; protected set; }

  public string Name { get; protected set; } = default!;

  public long? PlayerIdentityId { get; protected set; }
  
  public PlayerIdentity? PlayerIdentity { get; protected set; } = default!;
}
