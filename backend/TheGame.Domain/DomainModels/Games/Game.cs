using FluentValidation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games.Events;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public partial class Game : RootModel, IAuditedRecord
{
  public IReadOnlySet<LicensePlate> LicensePlates { get; private set; } = default!;
  protected HashSet<GameLicensePlate> _gameLicensePlates = [];
  public IReadOnlySet<GameLicensePlate> GameLicensePlates => _gameLicensePlates;

  public IReadOnlySet<Player> InvitedPlayers { get; private set; } = default!;
  protected HashSet<GamePlayer> _gamePlayerInvites = [];
  public IReadOnlySet<GamePlayer> GamePlayerInvites => _gamePlayerInvites;

  public GameScore GameScore { get; protected set; } = new(ReadOnlyCollection<string>.Empty, 0);

  public long Id { get; }

  public string Name { get; protected set; } = default!;

  public bool IsActive { get; protected set; }

  public long CreatedByPlayerId { get; protected set; }
  protected Player _createdBy = default!;
  public Player CreatedBy
  {
    get => _createdBy;
    protected set => _createdBy = value;
  }

  public DateTimeOffset? EndedOn { get; protected set; }

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }

  public Game() { }

  public HashSet<Player> GetActiveGamePlayers()
  {
    return GamePlayerInvites
      .Where(gp => gp.InviteStatus == GamePlayerInviteStatus.Accepted)
      .Select(gp => gp.Player)
      .Concat([CreatedBy])
      .ToHashSet();
  }

  public virtual Result<GamePlayer> InvitePlayer(IGamePlayerFactory gamePlayerFactory, Player playerToInvite)
  {
    var newGamePlayerResult = gamePlayerFactory.AddPlayer(playerToInvite, this);
    if (!newGamePlayerResult.TryGetSuccessful(out var newInvite, out var inviteFailure))
    {
      return inviteFailure;
    }
    
    _gamePlayerInvites.Add(newInvite);

    return newInvite;
  }

  public virtual Result<Game> UpdateLicensePlateSpots(IGameLicensePlateFactory licensePlateSpotFactory,
    TimeProvider timeProvider,
    IGameScoreCalculator scoreCalculator,
    IGameDbContext gameDbContext,
    GameLicensePlateSpots licensePlateSpots)
  {
    if (!IsActive)
    {
      return new Failure(ErrorMessageProvider.InactiveGameError);
    }

    if (!GetActiveGamePlayers().Contains(licensePlateSpots.SpottedBy))
    {
      return new Failure(ErrorMessageProvider.UninvitedPlayerError);
    }

    var existingSpotsLookup = this.GameLicensePlates
      .ToDictionary(spot => new LicensePlate.PlateKey(spot.LicensePlate.Country, spot.LicensePlate.StateOrProvince));

    // add new spots
    var newSpots = licensePlateSpots.Spots
      .Where(spot => !existingSpotsLookup.ContainsKey(spot))
      .ToList()
      .AsReadOnly();

    var gameDbInstance = gameDbContext as GameDbContext;

    foreach (var plateKey in newSpots)
    {
      var newSpotResult = licensePlateSpotFactory.CreateLicensePlateSpot(plateKey,
        licensePlateSpots.SpottedBy,
        timeProvider.GetUtcNow());
      
      if (!newSpotResult.TryGetSuccessful(out var newSpot, out var spotFailure))
      {
        return spotFailure;
      }

      _gameLicensePlates.Add(newSpot);
    }

    // remove any existing spots
    var updatedSpotsLookup = licensePlateSpots.Spots
      .ToHashSet();

    var spotsToRemove = existingSpotsLookup
      .Where(existingSpot => !updatedSpotsLookup.Contains(existingSpot.Key))
      .Select(existingSpot => existingSpot.Value)
      .ToHashSet();

    _gameLicensePlates.RemoveWhere(spotsToRemove.Contains);

    // update score and notify players if spots were updated
    if (newSpots.Count != 0 || spotsToRemove.Count != 0)
    {
      var allSpottedPlates = GameLicensePlates
        .Select(glp => new LicensePlate.PlateKey(glp.LicensePlate.Country, glp.LicensePlate.StateOrProvince ))
        .ToList()
        .AsReadOnly();
      
      var newScore = scoreCalculator.CalculateGameScore(allSpottedPlates);

      GameScore = GameScore with
      {
        Achievements = newScore.Achievements.ToList().AsReadOnly(),
        TotalScore = newScore.TotalScore
      };

      AddDomainEvent(new LicensePlateSpottedEvent(this));
    }

    return this;
  }

  public virtual Result<Game> EndGame()
  {
    if (!IsActive)
    {
      return new Failure(ErrorMessageProvider.InactiveGameError);
    }

    var endedOn = GameLicensePlates
      .Select(glp => glp.DateCreated)
      .OrderByDescending(dateSpotted => dateSpotted)
      .FirstOrDefault(DateCreated);

    IsActive = false;
    EndedOn = endedOn;

    AddDomainEvent(new ExistingGameFinishedEvent(this));

    return this;
  }
}

public sealed record GameLicensePlateSpots(IReadOnlyCollection<LicensePlate.PlateKey> Spots, Player SpottedBy);

public sealed record GameScore(ReadOnlyCollection<string> Achievements, int TotalScore);
