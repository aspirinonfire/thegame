using System.Collections.ObjectModel;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Games.Events
{
  public class LicensePlateSpottedEvent : IDomainEvent
  {
    public LicensePlateSpottedEvent(ReadOnlyCollection<LicensePlateSpot> licensePlateSpotModels)
    {
      LicensePlateSpotModels = licensePlateSpotModels;
    }

    public ReadOnlyCollection<LicensePlateSpot> LicensePlateSpotModels { get; }
  }
}
