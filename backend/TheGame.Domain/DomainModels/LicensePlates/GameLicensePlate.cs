using System;
using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates
{
  // TODO encapsulate!
  public partial class GameLicensePlate : BaseModel
  {
    public long LicensePlateId { get; set; }
    public virtual LicensePlate LicensePlate  { get; set; }

    public long GameId { get; set; }
    public virtual Game Game { get; set; }

    public virtual Player SpottedBy { get; set; }

    public DateTimeOffset DateCreated { get; set; }
  }
}
