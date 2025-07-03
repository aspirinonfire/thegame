using Microsoft.Extensions.DependencyInjection;

namespace TheGame.Api.Common.MessageBus;

public static class MessageBusServiceExtensions
{
  public static IServiceCollection AddInMemoryEventBus(this IServiceCollection services)
  {
    services
      .AddSingleton<ChannelsMessageQueue>()
      .AddSingleton<DomainMessagesProcessor>();

    return services;
  }
}
