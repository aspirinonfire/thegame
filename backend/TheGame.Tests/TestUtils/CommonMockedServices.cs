using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheGame.Api;
using TheGame.Api.CommandHandlers;
using TheGame.Domain;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Tests.TestUtils;

public static class CommonMockedServices
{
  public static readonly DateTimeOffset DefaultTestDate = new(2021, 12, 31, 0, 0, 0, 0, TimeSpan.Zero);

  public static TimeProvider GetMockedTimeProvider(DateTimeOffset? dateTimeOffset = null)
  {
    var sysSvc = Substitute.For<TimeProvider>();
    sysSvc.GetUtcNow().Returns(dateTimeOffset.GetValueOrDefault(DefaultTestDate));

    return sysSvc;
  }

  public static IServiceCollection GetGameServicesWithTestDevDb(string connString,
    Func<IServiceProvider, IEventBus>? busFactory = null)
  {
    if (busFactory is null)
    {
      var busMock = Substitute.For<IEventBus>();
      busMock
        .PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
        .Returns(Task.CompletedTask);

      busFactory = _ => busMock;
    }

    var services = new ServiceCollection()
      .AddGameServices(
        busFactory,
        connString,
        efLogger => efLogger.AddDebug())
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>()
      .AddLogging(builder => builder.AddDebug())
      .AddScoped<IGameQueryProvider, GameQueryProvider>();

    Program.AddCommandHandlers(services);

    return services;
  }

}
