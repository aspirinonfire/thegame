using Microsoft.Extensions.DependencyInjection;
using TheGame.Api.Common.MessageBus;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api.Common;

public static class CommonGameApiServices
{
  public static IServiceCollection AddGameApiCommonServices(this IServiceCollection services)
  {
    services
      .AddScoped<IGameQueryProvider, GameQueryProvider>()
      .AddInMemoryEventBus()
      .AddHostedService<DomainMessagesWorker>()
      .AddScoped(typeof(IDomainMessageHandler<>), typeof(DomainMessageLogger<>))
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>();

    return services;
  }
}
