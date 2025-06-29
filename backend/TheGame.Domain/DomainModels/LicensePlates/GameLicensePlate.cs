using System;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates;

public partial class GameLicensePlate
{
  // required for N:M EF Config
  public long LicensePlateId { get; protected set; }
  public LicensePlate LicensePlate { get; protected set; } = default!;

  // required for N:M EF Config
  public long GameId { get; protected set; }
  public Game Game { get; protected set; } = default!;

  public long SpottedByPlayerId { get; protected set; }
  public Player SpottedBy { get; protected set; } = default!;

  public DateTimeOffset DateCreated { get; protected set;  }
}
