using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games
{
  public partial class GameModel : BaseModel
  {
    public const string InactiveGameError = "inactive_game";
    public const string FailedToAddSpotError = "failed_to_add_spot";
    public const string InvalidEndedOnDate = "invalid_ended_on_date";

    protected HashSet<LicensePlateSpotModel> _licensePlateSpots = new();

    public IEnumerable<LicensePlateSpotModel> LicensePlateSpots => _licensePlateSpots;

    public long Id { get; }
    public string Name { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset? EndedOn { get; protected set; }

    public virtual Result<GameModel> AddLicensePlateSpot(ILicensePlateSpotFactory licensePlateSpotFactory,
      IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlateSpots,
      PlayerModel spottedBy)
    {
      if (!IsActive)
      {
        return Result.Error<GameModel>(InactiveGameError);
      }

      var existingSpots = LicensePlateSpots
        .Select(spot => (spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince))
        .ToHashSet();

      var newSpots = licensePlateSpots
        .Where(spot => !existingSpots.Contains(spot));

      var newSpottedPlates = new List<LicensePlateSpotModel>();
      foreach ((Country country, StateOrProvince stateOrProvince) in newSpots)
      {
        var licensePlateSpot = licensePlateSpotFactory.SpotLicensePlate(country,
          stateOrProvince,
          spottedBy);

        if (!licensePlateSpot.IsSuccess)
        {
          return Result.Error<GameModel>(FailedToAddSpotError);
        }

        newSpottedPlates.Add(licensePlateSpot.Value);
        GetWriteableCollection(LicensePlateSpots)
          .Add(licensePlateSpot.Value);
      }

      if (newSpottedPlates.Any())
      {
        AddEvent(new LicensePlateSpottedEvent(newSpottedPlates.AsReadOnly()));
      }

      return Result.Success(this);
    }

    public virtual Result<GameModel> RemoveLicensePlateSpot(
      IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlates,
      PlayerModel spottedBy)
    {
      if (!IsActive)
      {
        return Result.Error<GameModel>(InactiveGameError);
      }

      var toRemove = new HashSet<(Country country, StateOrProvince stateOrProvince)>(licensePlates);
      GetWriteableCollection(LicensePlateSpots)
        .RemoveWhere(spot => toRemove.Contains((spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince)));

      AddEvent(new LicensePlateSpotRemovedEvent());

      return Result.Success(this);
    }

    public virtual Result<GameModel> FinishGame(DateTimeOffset endedOn)
    {
      if (!IsActive)
      {
        return Result.Error<GameModel>(InactiveGameError);
      }

      if (endedOn < CreatedOn)
      {
        return Result.Error<GameModel>(InvalidEndedOnDate);
      }

      IsActive = false;
      EndedOn = endedOn;

      AddEvent(new ExistingGameFinishedEvent());

      return Result.Success(this);
    }
  }
}
