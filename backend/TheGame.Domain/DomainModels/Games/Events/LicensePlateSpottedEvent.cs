using System.Collections.ObjectModel;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Games.Events
{
  public class LicensePlateSpottedEvent : IDomainEvent
  {
    public LicensePlateSpottedEvent(ReadOnlyCollection<LicensePlateSpotModel> licensePlateSpotModels)
    {
      LicensePlateSpotModels = licensePlateSpotModels;
    }

    public ReadOnlyCollection<LicensePlateSpotModel> LicensePlateSpotModels { get; }
  }
}
