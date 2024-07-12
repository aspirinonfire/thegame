using System.Collections.Generic;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DomainModels.Games.Events;

public sealed record LicensePlateSpottedEvent(IReadOnlyCollection<GameLicensePlate> LicensePlateSpotModels) : IDomainEvent;
