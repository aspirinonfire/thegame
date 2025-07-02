using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheGame.Api;
using TheGame.Api.CommandHandlers;
using TheGame.Domain;

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

  public static IServiceCollection GetGameServicesWithTestDevDb(string connString)
  {
    var services = new ServiceCollection()
      .AddGameServices(connString,
        efLogger => efLogger.AddDebug())
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>()
      .AddLogging(builder => builder.AddDebug())
      .AddScoped<IGameQueryProvider, GameQueryProvider>();

    Program.AddCommandHandlers(services);

    return services;
  }

}
