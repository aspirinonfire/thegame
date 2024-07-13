using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Games.Events;

public sealed record ExistingGameFinishedEvent(Game game) : IDomainEvent;
