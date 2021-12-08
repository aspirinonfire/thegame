using System.Collections.Generic;
using thegame.domain.DomainModels.Common;
using thegame.domain.DomainModels.Team;

namespace thegame.domain.DomainModels.Player
{
  public partial class PlayerModel : BaseModel
  {
    protected HashSet<TeamModel> _team = new();

    public long UserId { get; protected set; }
    public string Name { get; protected set; }
  }
}
