using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public partial class GamePlayer : IAuditedRecord
{
  public long PlayerId { get; protected set; }
  public Player Player { get; protected set; } = default!;

  public long GameId { get; protected set; }
  public Game Game { get; protected set; } = default!;

  public Guid InvitationToken { get; protected set; } = default!;
  public GamePlayerInviteStatus InviteStatus { get; protected set; } = GamePlayerInviteStatus.Created;

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }

  protected GamePlayer() { }

  internal GamePlayer(Player playerToInvite) : this()
  {
    Player = playerToInvite;
    InvitationToken = Guid.NewGuid();
    InviteStatus = GamePlayerInviteStatus.Created;
  }
}

public enum GamePlayerInviteStatus
{
  Created = 0,
  InviteSent = 1,
  Accepted = 2,
  Rejected = 3,
  LeftMidGame = 4
}
