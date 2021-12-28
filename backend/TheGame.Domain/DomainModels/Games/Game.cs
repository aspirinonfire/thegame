using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games
{
  public partial class Game : BaseModel, IAuditedRecord
  {
    public const string InactiveGameError = "inactive_game";
    public const string FailedToAddSpotError = "failed_to_add_spot";
    public const string InvalidEndedOnDate = "invalid_ended_on_date";

    protected HashSet<LicensePlateSpot> _licensePlateSpots = new();

    // TODO turn this into N:M relationship with explicit intermediate model
    public ICollection<LicensePlateSpot> LicensePlateSpots => _licensePlateSpots;

    public long Id { get; }
    public string Name { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset? EndedOn { get; protected set; }

    public DateTimeOffset DateCreated { get; }

    public DateTimeOffset? DateModified { get; }

    public virtual Result<Game> AddLicensePlateSpot(ILicensePlateSpotFactory licensePlateSpotFactory,
      IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlateSpots,
      Player spottedBy)
    {
      if (!IsActive)
      {
        return Result.Error<Game>(InactiveGameError);
      }

      var existingSpots = LicensePlateSpots
        .Select(spot => (spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince))
        .ToHashSet();

      var newSpots = licensePlateSpots
        .Where(spot => !existingSpots.Contains(spot));

      var newSpottedPlates = new List<LicensePlateSpot>();
      foreach ((Country country, StateOrProvince stateOrProvince) in newSpots)
      {
        var licensePlateSpot = licensePlateSpotFactory.SpotLicensePlate(country,
          stateOrProvince,
          spottedBy);

        if (!licensePlateSpot.IsSuccess)
        {
          return Result.Error<Game>(FailedToAddSpotError);
        }

        newSpottedPlates.Add(licensePlateSpot.Value);
        GetWriteableCollection(LicensePlateSpots)
          .Add(licensePlateSpot.Value);
      }

      if (newSpottedPlates.Any())
      {
        AddDomainEvent(new LicensePlateSpottedEvent(newSpottedPlates.AsReadOnly()));
      }

      return Result.Success(this);
    }

    public virtual Result<Game> RemoveLicensePlateSpot(
      IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlatesToRemove,
      Player spottedBy)
    {
      if (!IsActive)
      {
        return Result.Error<Game>(InactiveGameError);
      }

      var toRemove = new HashSet<(Country country, StateOrProvince stateOrProvince)>(licensePlatesToRemove);
      GetWriteableCollection(LicensePlateSpots)
        .RemoveWhere(spot => toRemove.Contains((spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince)));

      AddDomainEvent(new LicensePlateSpotRemovedEvent(licensePlatesToRemove));

      return Result.Success(this);
    }

    public virtual Result<Game> FinishGame(DateTimeOffset endedOn)
    {
      if (!IsActive)
      {
        return Result.Error<Game>(InactiveGameError);
      }

      if (endedOn < DateCreated)
      {
        return Result.Error<Game>(InvalidEndedOnDate);
      }

      IsActive = false;
      EndedOn = endedOn;

      AddDomainEvent(new ExistingGameFinishedEvent());

      return Result.Success(this);
    }
  }
}
