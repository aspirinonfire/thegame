using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TheGame.Domain.DAL;

namespace TheGame.Tests.Fixtures
{
  public class MsSqlFixture : IAsyncLifetime
  {
    private readonly MsSqlContainer _msSqlContainer;

    public string GetConnectionString() => _msSqlContainer.GetConnectionString();

    public MsSqlFixture()
    {
      _msSqlContainer = new MsSqlBuilder().Build();
    }

    /// <summary>
    /// Start MSSQL and run migrations
    /// </summary>
    /// <returns></returns>
    public async Task InitializeAsync()
    {
      await _msSqlContainer.StartAsync();

      var services = DevTestFixture.GetGameServicesWithTestDevDb(GetConnectionString());
      await using var sp = services.BuildServiceProvider();
      var dbContext = sp.GetRequiredService<GameDbContext>();
      await dbContext.Database.MigrateAsync();
    }

    /// <inheritdoc/>
    public Task DisposeAsync() => _msSqlContainer.DisposeAsync().AsTask();
  }
}
