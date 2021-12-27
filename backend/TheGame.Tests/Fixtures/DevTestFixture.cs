using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using TheGame.Domain;
using TheGame.Domain.DAL;

namespace TheGame.Tests.Fixtures
{
  public class DevTestFixture : IDisposable
  {
    private readonly IConfigurationRoot _config;

    public string ConnString => _config.GetConnectionString(GameDbContext.ConnectionStringName);

    public DevTestFixture()
    {
      _config = new ConfigurationBuilder()
        .AddJsonFile("testsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("testsettings.override.json", optional: true, reloadOnChange: true)
        .Build();
    }

    public IServiceCollection GetGameServicesWithTestDevDb() =>
      new ServiceCollection()
        .AddGameServices(ConnString, true);

    public void Dispose()
    {
      // noop for now
    }
  }
}
