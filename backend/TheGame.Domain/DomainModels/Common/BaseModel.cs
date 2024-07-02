using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.Common;

public class BaseModel : IDomainModel
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

  protected static HashSet<T> GetWriteableCollection<T>(IEnumerable<T> navCollection) where T: BaseModel
  {
    if (navCollection == null)
    {
      return [];
    }

    if (navCollection is HashSet<T> writeable)
    {
      return writeable;
    }
    return new HashSet<T>(navCollection);
  }
}
