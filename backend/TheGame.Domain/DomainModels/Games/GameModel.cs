using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Domain.DomainModels.Games
{
  public partial class GameModel : BaseModel
  {
    public const string InactiveGameError = "inactive_game";

    protected TeamModel _team;
    protected HashSet<LicensePlateSpotModel> _licensePlates = new();

    public IEnumerable<LicensePlateSpotModel> LicensePlates => _licensePlates;

    public long Id { get; }
    public string Name { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset? EndedOn { get; protected set; }

    public virtual Result<LicensePlateSpotModel> SpotLicensePlate(ILicensePlateSpotFactory licensePlateSpotFactory,
      LicensePlateModel licensePlate,
      PlayerModel spottedBy)
    {
      if (!IsActive)
      {
        return Result.Error<LicensePlateSpotModel>(InactiveGameError);
      }

      var plateSpotResult = licensePlateSpotFactory.SpotLicensePlate(licensePlate, spottedBy);
      if (!plateSpotResult.IsSuccess)
      {
        return plateSpotResult;
      }

      GetWriteableCollection(LicensePlates)
        .Add(plateSpotResult.Value);

      return plateSpotResult;
    }

    public virtual Result<GameModel> FinishGame(DateTimeOffset endedOn)
    {
      if (!IsActive)
      {
        return Result.Error<GameModel>(InactiveGameError);
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
