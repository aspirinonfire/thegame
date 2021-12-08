using System;
using System.Collections.Generic;
using thegame.domain.DomainModels.Common;
using thegame.domain.DomainModels.LicensePlate;
using thegame.domain.DomainModels.Team;

namespace thegame.domain.DomainModels.Game
{
  public partial class GameModel : BaseModel
  {
    protected TeamModel _team;
    protected HashSet<LicensePlateModel> _licensePlate = new();

    public long Id { get; }
    public string Name { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset? EndedOn { get; protected set; }

    public Result<GameModel> EndGame(DateTimeOffset endedOn)
    {
      if (!IsActive)
      {
        return Result.Error<GameModel>("inactive_game");
      }

      if (endedOn < CreatedOn)
      {
        return Result.Error<GameModel>("invalid_ended_on_date");
      }

      IsActive = false;
      EndedOn = endedOn;

      return Result.Success(this);
    }
  }
}
