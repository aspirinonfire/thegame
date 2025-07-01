using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

  public static IServiceCollection GetGameServicesWithTestDevDb(string connString) =>
    new ServiceCollection()
      .AddGameServices(connString,
        typeof(TheGame.Api.Program).Assembly,
        efLogger => efLogger.AddDebug())
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>()
      .AddLogging(builder => builder.AddDebug());
}
