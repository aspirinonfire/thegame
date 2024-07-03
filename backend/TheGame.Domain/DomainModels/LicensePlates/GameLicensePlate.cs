using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates;

public partial class GameLicensePlate : BaseModel
{
  // required for N:M EF Config
  public long LicensePlateId { get; protected set; }
  public virtual LicensePlate LicensePlate { get; protected set; } = default!;

  // required for N:M EF Config
  public long GameId { get; protected set; }
  public virtual Game Game { get; protected set; } = default!;

  public long SpottedByPlayerId { get; protected set; }
  public virtual Player SpottedBy { get; protected set; } = default!;

  public DateTimeOffset DateCreated { get; protected set; } = default!;
}
