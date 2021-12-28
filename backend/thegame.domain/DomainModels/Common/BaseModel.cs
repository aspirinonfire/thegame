using System;
using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.Common
{
  public class BaseModel : IDomainModel
  {
    private readonly HashSet<IDomainEvent> _domainEvents = new();
    private readonly HashSet<IIntegrationEvent> _integrationEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;
    public IReadOnlyCollection<IIntegrationEvent> IntegrationEvents => _integrationEvents;

    /// <summary>
    /// Add an event to be handled by this microservice
    /// </summary>
    /// <param name="domainEvent"></param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
      _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Add an event to be handled by external system. E.g. SendGrid, SignalR, ServiceBus
    /// </summary>
    /// <param name="domainEvent"></param>
    protected void AddIntegrationEvent(IIntegrationEvent domainEvent)
    {
      _integrationEvents.Add(domainEvent);
    }

    protected static HashSet<T> GetWriteableCollection<T>(IEnumerable<T> navCollection) where T: BaseModel
    {
      if (navCollection == null)
      {
        return new HashSet<T>();
      }

      if (navCollection is HashSet<T> writeable)
      {
        return writeable;
      }
      return new HashSet<T>(navCollection);
    }
  }
}
