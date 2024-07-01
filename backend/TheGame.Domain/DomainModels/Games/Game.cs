using FluentValidation;
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
    public const string InvalidEndedOnDateError = "invalid_ended_on_date";
    public const string UninvitedPlayerError = "invalid_player";
  }

  public virtual ICollection<LicensePlate> LicensePlates { get; private set; } = [];
  protected HashSet<GameLicensePlate> _gameLicensePlates = [];
  public virtual ICollection<GameLicensePlate> GameLicensePlates => _gameLicensePlates;

  public virtual ICollection<Player> InvitedPlayers { get; private set; } = [];
  protected HashSet<GamePlayer> _gamePlayerInvites = [];
  public virtual ICollection<GamePlayer> GamePlayerInvites => _gamePlayerInvites;

  public long Id { get; }

  public string Name { get; protected set; } = default!;

  public bool IsActive { get; protected set; }

  protected Player _createdBy = default!;
  public virtual Player CreatedBy
  {
    get => _createdBy;
    protected set => _createdBy = value;
  }

  public DateTimeOffset? EndedOn { get; protected set; }

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }

  public Game() { }

  public virtual HashSet<Player> GetActiveGamePlayers()
  {
    return GamePlayerInvites
      .Where(gp => gp.InviteStatus == GamePlayerInviteStatus.Accepted)
      .Select(gp => gp.Player)
      .Concat([CreatedBy])
      .ToHashSet();
  }

  public virtual OneOf<GamePlayer, Failure> InvitePlayer(IGamePlayerFactory gamePlayerFactory, Player playerToInvite)
  {
    var newGamePlayerResult = gamePlayerFactory.AddPlayer(playerToInvite, this);
    if (!newGamePlayerResult.TryGetSuccessful(out var successfulInvite, out var inviteFailure))
    {
      return inviteFailure;
    }

    return successfulInvite;
  }

  public virtual OneOf<Game, Failure> AddLicensePlateSpot(IGameLicensePlateFactory licensePlateSpotFactory,
    ISystemService systemService,
    IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlateSpots,
    Player spottedBy)
  {
    if (!IsActive)
    {
      return new Failure(ErrorMessages.InactiveGameError);
    }

    if (!GetActiveGamePlayers().Contains(spottedBy))
    {
      return new Failure(ErrorMessages.UninvitedPlayerError);
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

    if (!GetActiveGamePlayers().Contains(spottedBy))
    {
      return new Failure(ErrorMessages.UninvitedPlayerError);
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
      return new Failure(ErrorMessages.InvalidEndedOnDateError);
    }

    IsActive = false;
    EndedOn = endedOn;

    AddDomainEvent(new ExistingGameFinishedEvent());

    return this;
  }
}
