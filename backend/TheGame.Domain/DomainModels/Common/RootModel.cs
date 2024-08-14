using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.Common;

public abstract class RootModel : IDomainModel
{
  private readonly HashSet<IDomainEvent> _domainEvents = [];
  public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

  /// <summary>
  /// Add an event to be handled by this microservice
  /// </summary>
  /// <param name="domainEvent"></param>
  protected void AddDomainEvent(IDomainEvent domainEvent)
  {
    _domainEvents.Add(domainEvent);
  }
}
