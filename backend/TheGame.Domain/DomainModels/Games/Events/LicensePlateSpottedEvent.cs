using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Games.Events;

public sealed record LicensePlateSpottedEvent(Game Game) : IDomainEvent;
