using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Domain.DomainModels.Games
{
  public partial class Game : BaseModel, IAuditedRecord
  {
    public const string InactiveGameError = "inactive_game";
    public const string FailedToAddSpotError = "failed_to_add_spot";
    public const string InvalidEndedOnDate = "invalid_ended_on_date";

    protected HashSet<GameLicensePlate> _gameLicensePlates = new();

    // TODO hide rela from CLR somehow. Use GameLicensePlates
    public virtual ICollection<LicensePlate> LicensePlates { get; protected set; }
    public virtual ICollection<GameLicensePlate> GameLicensePlates => _gameLicensePlates;

    public long Id { get; }
    public string Name { get; protected set; }
    public bool IsActive { get; protected set; }
    public DateTimeOffset? EndedOn { get; protected set; }

    public DateTimeOffset DateCreated { get; }

    public DateTimeOffset? DateModified { get; }

    public virtual Result<Game> AddLicensePlateSpot(IGameLicensePlateFactory licensePlateSpotFactory,
      ISystemService systemService,
      IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlateSpots,
      Player spottedBy)
    {
      if (!IsActive)
      {
        return Result.Error<Game>(InactiveGameError);
      }

      var existingSpots = GameLicensePlates
        .Select(spot => (spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince))
        .ToHashSet();

      var newSpots = licensePlateSpots
        .Where(spot => !existingSpots.Contains(spot));

      var newSpottedPlates = new List<GameLicensePlate>();
      foreach ((Country country, StateOrProvince stateOrProvince) in newSpots)
      {
        var licensePlateSpot = licensePlateSpotFactory.CreateLicensePlateSpot(country,
          stateOrProvince,
          spottedBy,
          systemService.DateTimeOffset.UtcNow);

        if (!licensePlateSpot.IsSuccess)
        {
          return Result.Error<Game>(FailedToAddSpotError);
        }

        newSpottedPlates.Add(licensePlateSpot.Value);
        GetWriteableCollection(GameLicensePlates)
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
      GetWriteableCollection(GameLicensePlates)
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
