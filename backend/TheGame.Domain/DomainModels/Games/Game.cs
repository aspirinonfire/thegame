using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Domain.DomainModels.Games;

public partial class Game : BaseModel, IAuditedRecord
{
  public static class ErrorMessages
  {
    public const string InactiveGameError = "inactive_game";
    public const string FailedToAddSpotError = "failed_to_add_spot";
    public const string InvalidEndedOnDate = "invalid_ended_on_date";
  }

  /// <summary>
  /// Do not use this navigation. Use GameLicensePlates instead.
  /// LicensePlate navigation property is required for EF Core 6 configuration
  /// </summary>
  [Obsolete("Use for EF config only! Might be removed in future EF 7+")]
  public virtual ICollection<LicensePlate> LicensePlates { get; private set; }

  protected HashSet<GameLicensePlate> _gameLicensePlates = new();
  public virtual ICollection<GameLicensePlate> GameLicensePlates => _gameLicensePlates;

  public long Id { get; }
  public string Name { get; protected set; }
  public bool IsActive { get; protected set; }
  public DateTimeOffset? EndedOn { get; protected set; }

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }

  public Game()
  {
    // Autopopulated by EF
    Name = null!;
    LicensePlates = null!;
  }

  public virtual DomainResult<Game> AddLicensePlateSpot(IGameLicensePlateFactory licensePlateSpotFactory,
    ISystemService systemService,
    IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlateSpots,
    Player spottedBy)
  {
    if (!IsActive)
    {
      return DomainResult.Error<Game>(ErrorMessages.InactiveGameError);
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

      if (!licensePlateSpotResult.IsSuccess || licensePlateSpotResult.HasNoValue)
      {
        return DomainResult.Error<Game>(ErrorMessages.FailedToAddSpotError);
      }

      newSpottedPlates.Add(licensePlateSpotResult.Value!);
      GetWriteableCollection(GameLicensePlates)
        .Add(licensePlateSpotResult.Value!);
    }

    if (newSpottedPlates.Any())
    {
      AddDomainEvent(new LicensePlateSpottedEvent(newSpottedPlates.AsReadOnly()));
    }

    return DomainResult.Success(this);
  }

  public virtual DomainResult<Game> RemoveLicensePlateSpot(
    IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlatesToRemove,
    Player spottedBy)
  {
    if (!IsActive)
    {
      return DomainResult.Error<Game>(ErrorMessages.InactiveGameError);
    }

    var toRemove = new HashSet<(Country country, StateOrProvince stateOrProvince)>(licensePlatesToRemove);
    GetWriteableCollection(GameLicensePlates)
      .RemoveWhere(spot => toRemove.Contains((spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince)));

    AddDomainEvent(new LicensePlateSpotRemovedEvent(licensePlatesToRemove));

    return DomainResult.Success(this);
  }

  public virtual DomainResult<Game> FinishGame(DateTimeOffset endedOn)
  {
    if (!IsActive)
    {
      return DomainResult.Error<Game>(ErrorMessages.InactiveGameError);
    }

    if (endedOn < DateCreated)
    {
      return DomainResult.Error<Game>(ErrorMessages.InvalidEndedOnDate);
    }

    IsActive = false;
    EndedOn = endedOn;

    AddDomainEvent(new ExistingGameFinishedEvent());

    return DomainResult.Success(this);
  }
}
