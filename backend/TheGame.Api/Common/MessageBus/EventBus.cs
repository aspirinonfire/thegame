using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api.Common.MessageBus;

public interface IEventBus
{
  Task PublishAsync<T>(T integrationEvent, CancellationToken cancellationToken = default)
    where T : class, IDomainEvent;
}

public sealed class EventBus(InMemoryMessageQueue queue) : IEventBus
{
  public async Task PublishAsync<T>(
      T integrationEvent,
      CancellationToken cancellationToken = default)
      where T : class, IDomainEvent
  {
    await queue.Writer.WriteAsync(integrationEvent, cancellationToken);
  }
}