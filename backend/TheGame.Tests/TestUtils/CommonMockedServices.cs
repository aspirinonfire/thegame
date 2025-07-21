using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheGame.Api.Auth;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.Game;
using TheGame.Api.Endpoints.User;
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

  public static IServiceCollection GetGameServicesWithTestDevDb(string connString)
  {
    Func<IServiceProvider, IEventBus> busFactory = sp =>
    {
      var logger = sp.GetRequiredService<ILogger<IEventBus>>();

      var busMock = Substitute.For<IEventBus>();
      busMock
        .PublishAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>())
        .Returns(Task.CompletedTask)
        .AndDoes(callInfo =>
        {
          logger.LogInformation("Mocked event bus published event {EventType}",
            callInfo.Arg<IDomainEvent>().GetType().Name);
        });

      return busMock;
    };

    var services = new ServiceCollection()
      .AddGameServices(
        busFactory,
        connString,
        efLogger => efLogger.AddDebug())
      .AddLogging(builder =>
      {
        builder.SetMinimumLevel(LogLevel.Information);
        builder.AddDebug();
      })
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>()
      .AddScoped(typeof(IDomainMessageHandler<>), typeof(DomainMessageLogger<>))
      .AddScoped<IGameQueryProvider, GameQueryProvider>()
      .AddScoped(sp => Substitute.For<IGoogleAuthService>())
      .AddScoped(sp =>
      {
        return Substitute.For<IGameAuthService>();
      })
      .AddUserEndpointServices()
      .AddGameEndpointServices();

    return services;
  }
}
