using MediatR;
using System.Collections.Generic;

namespace TheGame.Domain.DomainModels.Common;

public interface IDomainEvent : INotification
{ }

public interface IDomainModel
{
  IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
