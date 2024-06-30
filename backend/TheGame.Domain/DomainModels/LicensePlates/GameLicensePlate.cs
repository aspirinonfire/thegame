using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates;

public partial class GameLicensePlate : BaseModel
{
  // required for N:M EF Config
  public long LicensePlateId { get; protected set; }
  public virtual LicensePlate LicensePlate  { get; protected set; }

  // required for N:M EF Config
  public long GameId { get; protected set; }
  public virtual Game Game { get; protected set; }

  public virtual Player SpottedBy { get; protected set; }

  public DateTimeOffset DateCreated { get; protected set; }

  public GameLicensePlate()
  {
    // Autopopulated by EF
    LicensePlate = null!;
    Game = null!;
    SpottedBy = null!;
  }
}
