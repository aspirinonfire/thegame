using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain;

namespace TheGame.Tests.Fixtures
{
  public class DevTestFixture
  {
    public IConfigurationRoot TestConfig { get; }

    public DevTestFixture()
    {
      TestConfig = new ConfigurationBuilder()
        .AddJsonFile("testsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("testsettings.override.json", optional: true, reloadOnChange: true)
        .Build();
    }

    public static IServiceCollection GetGameServicesWithTestDevDb(string connString) =>
      new ServiceCollection()
        .AddGameServices(connString, true);
  }
}
