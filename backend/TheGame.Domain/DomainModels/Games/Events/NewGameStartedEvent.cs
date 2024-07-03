using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Games.Events;

public sealed record NewGameStartedEvent(string GameName, long StartedByPlayerId) : IDomainEvent;
