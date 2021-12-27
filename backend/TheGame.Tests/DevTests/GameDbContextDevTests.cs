using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using TheGame.Domain;
using TheGame.Domain.DAL;
using TheGame.Tests.Fixtures;
using TheGame.Tests.TestUtils;
using Xunit;

namespace TheGame.Tests.DevTests
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.DevTest)]
  public class GameDbContextDevTests: IClassFixture<DevTestFixture>
  {
    private readonly DevTestFixture _fixture;

    public GameDbContextDevTests(DevTestFixture fixture)
    {
      _fixture = fixture;
    }

    [Fact]
    public async Task CanQueryLicensePlates()
    {
      var services = _fixture.GetGameServicesWithTestDevDb();

      var diOpts = new ServiceProviderOptions
      {
        ValidateOnBuild = true,
        ValidateScopes = true,
      };
      using var sp = services.BuildServiceProvider(diOpts);
      using var scope = sp.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

      var plates = await db.LicensePlates.ToListAsync();
      Assert.NotEmpty(plates);
    }
  }
}
