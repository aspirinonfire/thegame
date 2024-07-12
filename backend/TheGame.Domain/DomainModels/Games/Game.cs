using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public partial class Game : RootModel, IAuditedRecord
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

  public long CreatedByPlayerId { get; protected set; }
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

  public virtual OneOf<Game, Failure> UpdateLicensePlateSpots(IGameLicensePlateFactory licensePlateSpotFactory,
    ISystemService systemService,
    GameLicensePlateSpots licensePlateSpots)
  {
    if (!IsActive)
    {
      return new Failure(ErrorMessages.InactiveGameError);
    }

    if (!GetActiveGamePlayers().Contains(licensePlateSpots.SpottedBy))
    {
      return new Failure(ErrorMessages.UninvitedPlayerError);
    }

    var writeableGameSpots = GameLicensePlates.GetWriteableCollection();

    var existingSpotsLookup = this.GameLicensePlates
      .ToDictionary(spot => (spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince));

    // add new spots
    var newSpots = licensePlateSpots.Spots
      .Where(spot => !existingSpotsLookup.ContainsKey(spot))
      .ToList()
      .AsReadOnly();

    foreach (var (country, stateOrProvince) in newSpots)
    {
      var newSpotResult = licensePlateSpotFactory.CreateLicensePlateSpot(country,
        stateOrProvince,
        licensePlateSpots.SpottedBy,
        systemService.DateTimeOffset.UtcNow);
      
      if (!newSpotResult.TryGetSuccessful(out var newSpot, out var spotFailure))
      {
        return spotFailure;
      }
      
      writeableGameSpots.Add(newSpot);
    }

    // remove any existing spots
    var updatedSpotsLookup = licensePlateSpots.Spots
      .ToHashSet();

    var spotsToRemove = existingSpotsLookup
      .Values
      .Where(existingSpot => !updatedSpotsLookup.Contains((existingSpot.LicensePlate.Country, existingSpot.LicensePlate.StateOrProvince)))
      .ToList()
      .AsReadOnly();
    
    foreach (var toRemove in spotsToRemove)
    {
      writeableGameSpots.Remove(toRemove);
    }

    // notify players if spots were updated
    if (newSpots.Count != 0 || spotsToRemove.Count != 0)
    {
      AddDomainEvent(new LicensePlateSpottedEvent(this));
    }

    return this;
  }

  public virtual OneOf<Game, Failure> EndGame(DateTimeOffset endedOn)
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

    AddDomainEvent(new ExistingGameFinishedEvent(this));

    return this;
  }
}

public sealed record GameLicensePlateSpots(IReadOnlyCollection<(Country country, StateOrProvince stateOrProvince)> Spots, Player SpottedBy);
