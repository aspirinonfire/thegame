using Microsoft.Extensions.DependencyInjection;

namespace TheGame.Api.Common.MessageBus;

public static class MessageBusServiceExtensions
{
  public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
  {
    services
      .AddSingleton<InMemoryMessageQueue>()
      .AddSingleton<IEventBus, EventBus>()
      .AddSingleton<DomainMessagesProcessor>();

    return services;
  }
}
