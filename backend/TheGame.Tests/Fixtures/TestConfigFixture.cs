using Microsoft.Extensions.Configuration;

namespace TheGame.Tests.Fixtures
{
  public class TestConfigFixture
  {
    public IConfigurationRoot TestConfig { get; }

    public TestConfigFixture()
    {
      TestConfig = new ConfigurationBuilder()
        .AddJsonFile("testsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile("testsettings.override.json", optional: true, reloadOnChange: true)
        .Build();
    }
  }
}
