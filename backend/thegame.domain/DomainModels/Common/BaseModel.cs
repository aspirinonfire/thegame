using System;
using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.Common
{
  public class BaseModel
  {
    private HashSet<BaseDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<BaseDomainEvent> DomainEvents => _domainEvents;

    public DateTimeOffset? CreatedOn { get; }
    public long? CreatedBy { get; }
    public DateTimeOffset? ModifiedOn { get; }
    public long? ModifiedBy { get; }

    protected void AddEvent(BaseDomainEvent domainEvent)
    {
      _domainEvents.Add(domainEvent);
    }
  }
}
