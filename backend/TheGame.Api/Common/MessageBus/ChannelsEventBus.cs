using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api.Common.MessageBus;

public sealed class ChannelsEventBus(ChannelsMessageQueue queue) : IEventBus
{
  public async Task PublishAsync<T>(
      T integrationEvent,
      CancellationToken cancellationToken = default)
      where T : class, IDomainEvent
  {
    await queue.Writer.WriteAsync(integrationEvent, cancellationToken);
  }
}