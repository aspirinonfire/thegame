using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheGame.Domain.DomainModels.Common
{
  public interface IDomainModel
  {
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    IReadOnlyCollection<IIntegrationEvent> IntegrationEvents { get; }
  }
}
