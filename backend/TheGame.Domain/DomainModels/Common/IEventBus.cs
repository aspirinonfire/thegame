using System.Threading;
using System.Threading.Tasks;

namespace TheGame.Domain.DomainModels.Common;

public interface IEventBus
{
  Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
    where T : class, IDomainEvent;
}
