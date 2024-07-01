using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public partial class Game : BaseModel, IAuditedRecord
{
  public static class ErrorMessages
  {
    public const string InactiveGameError = "inactive_game";
    public const string FailedToAddSpotError = "failed_to_add_spot";
    public const string InvalidEndedOnDate = "invalid_ended_on_date";
  }

  public virtual ICollection<LicensePlate> LicensePlates { get; private set; } = default!;
  protected HashSet<GameLicensePlate> _gameLicensePlates = [];
  public virtual ICollection<GameLicensePlate> GameLicensePlates => _gameLicensePlates;

  public long Id { get; }
  public string Name { get; protected set; } = default!;
  public bool IsActive { get; protected set; }
  public DateTimeOffset? EndedOn { get; protected set; }

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }

  public Game() { }

  public virtual OneOf<Game, Failure> AddLicensePlateSpot(IGameLicensePlateFactory licensePlateSpotFactory,
    ISystemService systemService,
    IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlateSpots,
    Player spottedBy)
  {
    if (!IsActive)
    {
      return new Failure(ErrorMessages.InactiveGameError);
    }

    var existingSpots = GameLicensePlates
      .Select(spot => (spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince))
      .ToHashSet();

    var newSpots = licensePlateSpots
      .Where(spot => !existingSpots.Contains(spot));

    var newSpottedPlates = new List<GameLicensePlate>();
    foreach ((Country country, StateOrProvince stateOrProvince) in newSpots)
    {
      var licensePlateSpotResult = licensePlateSpotFactory.CreateLicensePlateSpot(country,
        stateOrProvince,
        spottedBy,
        systemService.DateTimeOffset.UtcNow);

      if (!licensePlateSpotResult.TryGetSuccessful(out var successfulSpot, out var spotFailure))
      {
        return spotFailure;
      }

      newSpottedPlates.Add(successfulSpot);
      GetWriteableCollection(GameLicensePlates)
        .Add(successfulSpot);
    }

    if (newSpottedPlates.Count != 0)
    {
      AddDomainEvent(new LicensePlateSpottedEvent(newSpottedPlates.AsReadOnly()));
    }

    return this;
  }

  public virtual OneOf<Game, Failure> RemoveLicensePlateSpot(
    IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlatesToRemove,
    Player spottedBy)
  {
    if (!IsActive)
    {
      return new Failure(ErrorMessages.InactiveGameError);
    }

    var toRemove = new HashSet<(Country country, StateOrProvince stateOrProvince)>(licensePlatesToRemove);
    GetWriteableCollection(GameLicensePlates)
      .RemoveWhere(spot => toRemove.Contains((spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince)));

    AddDomainEvent(new LicensePlateSpotRemovedEvent(licensePlatesToRemove));

    return this;
  }

  public virtual OneOf<Game, Failure> FinishGame(DateTimeOffset endedOn)
  {
    if (!IsActive)
    {
      return new Failure(ErrorMessages.InactiveGameError);
    }

    if (endedOn < DateCreated)
    {
      return new Failure(ErrorMessages.InvalidEndedOnDate);
    }

    IsActive = false;
    EndedOn = endedOn;

    AddDomainEvent(new ExistingGameFinishedEvent());

    return this;
  }
}
