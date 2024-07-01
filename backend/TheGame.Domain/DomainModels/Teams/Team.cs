using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Teams;

public partial class Team : BaseModel
{
  protected HashSet<Player> _players = [];
  protected HashSet<Game> _games = [];

  public virtual ICollection<Game> Games => _games;
  public virtual ICollection<Player> Players => _players;

  public long Id { get; }
  public string Name { get; protected set; } = default!;

  public Team() { }
}
