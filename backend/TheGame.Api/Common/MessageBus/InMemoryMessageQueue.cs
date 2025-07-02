using Microsoft.Extensions.Options;
using System;
using System.Threading.Channels;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api.Common.MessageBus;

public sealed class InMemoryMessageQueue : IDisposable
{
  private readonly Channel<IDomainEvent> _channel;

  public ChannelReader<IDomainEvent> Reader => _channel.Reader;

  public ChannelWriter<IDomainEvent> Writer => _channel.Writer;

  public InMemoryMessageQueue(IOptions<GameSettings> apiSettings)
  {
    _channel = Channel.CreateBounded<IDomainEvent>(
      new BoundedChannelOptions(apiSettings.Value.DomainEventsMessageBus.MaxQueueSize)
      {
        SingleReader = true,
        SingleWriter = false,
        AllowSynchronousContinuations = false,
        FullMode = BoundedChannelFullMode.Wait
      });
  }

  public void Dispose()
  {
    _channel.Writer.TryComplete();
  }
}
