using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.Common;

public interface IDomainEvent
{ }

public interface IDomainModel
{
  IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
