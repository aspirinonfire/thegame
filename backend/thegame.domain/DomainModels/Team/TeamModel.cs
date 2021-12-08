using System.Collections.Generic;
using System.Linq;
using thegame.domain.DomainModels.Common;
using thegame.domain.DomainModels.Game;
using thegame.domain.DomainModels.Player;

namespace thegame.domain.DomainModels.Team
{
  public partial class TeamModel : BaseModel
  {

    protected HashSet<PlayerModel> _players = new();
    protected HashSet<GameModel> _games = new();

    public long Id { get; }
    public string Name { get; protected set; }

    public Result<PlayerModel> AddPlayer(PlayerModel player)
    {
      _players.Add(player);
      return Result.Success(player);
    }
  }
}
