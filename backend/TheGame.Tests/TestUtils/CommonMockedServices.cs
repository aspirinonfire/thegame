using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheGame.Domain;

namespace TheGame.Tests.TestUtils
{
  public static class CommonMockedServices
  {
    public static readonly DateTimeOffset DefaultTestDate = new(2021, 12, 31, 0, 0, 0, 0, TimeSpan.Zero);

    public static ISystemService GetSystemService(DateTimeOffset? dateTimeOffset = null)
    {
      var currentTimestamp = dateTimeOffset.GetValueOrDefault(DefaultTestDate);

      var dtOffsetSvc = Substitute.For<IDateTimeOffsetService>();
      dtOffsetSvc.UtcNow.Returns(currentTimestamp);
      dtOffsetSvc.Now.Returns(currentTimestamp);

      var dtSvc = Substitute.For<IDateTimeService>();
      dtSvc.Now.Returns(currentTimestamp.DateTime);

      var sysSvc = Substitute.For<ISystemService>();
      sysSvc.DateTimeOffset.Returns(dtOffsetSvc);
      sysSvc.DateTime.Returns(dtSvc);

      return sysSvc;
    }

    public static IServiceCollection GetGameServicesWithTestDevDb(string connString) =>
      new ServiceCollection()
        .AddGameServices(connString, true)
        .AddLogging(builder => builder.AddDebug());
  }
}
