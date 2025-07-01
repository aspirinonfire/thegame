using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.Games;

public partial class Game : IAuditedRecord
{
  public IReadOnlySet<LicensePlate> LicensePlates { get; private set; } = default!;
  internal HashSet<GameLicensePlate> _gameLicensePlates = [];
  public IReadOnlySet<GameLicensePlate> GameLicensePlates => _gameLicensePlates;

  public IReadOnlySet<Player> InvitedPlayers { get; private set; } = default!;
  internal HashSet<GamePlayer> _gamePlayers = [];
  public IReadOnlySet<GamePlayer> GamePlayers => _gamePlayers;

  public GameScore GameScore { get; internal set; } = new(ReadOnlyCollection<string>.Empty, 0);

  public long Id { get; protected set; }

  public string Name { get; internal set; } = default!;

  public bool IsActive { get; internal set; }

  public long CreatedByPlayerId { get; internal set; }
  protected Player _createdBy = default!;
  public Player CreatedBy
  {
    get => _createdBy;
    internal set => _createdBy = value;
  }

  public DateTimeOffset? EndedOn { get; internal set; }

  public DateTimeOffset DateCreated { get; }

  public DateTimeOffset? DateModified { get; }

  internal Game() { }
}

public sealed record GameScore(ReadOnlyCollection<string> Achievements, int TotalScore);
