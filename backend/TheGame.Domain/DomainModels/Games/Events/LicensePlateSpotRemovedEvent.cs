using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Games.Events
{
  public class LicensePlateSpotRemovedEvent : BaseDomainEvent
  {
    public LicensePlateSpotRemovedEvent(IEnumerable<(Country country, StateOrProvince stateOrProvince)> licensePlatesToRemove)
    {
      LicensePlatesToRemove = licensePlatesToRemove;
    }

    public IEnumerable<(Country country, StateOrProvince stateOrProvince)> LicensePlatesToRemove { get; }
  }
}
