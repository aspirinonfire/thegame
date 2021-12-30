using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Threading.Tasks;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Teams;
using TheGame.Domain.Utils;
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
      var db = scope.ServiceProvider.GetRequiredService<IGameDbContext>();

      var plates = await db.LicensePlates.ToListAsync();
      Assert.NotEmpty(plates);
    }

    [Fact]
    public async Task CreateTeamPlayerGameAndAddSpot()
    {
      var services = _fixture.GetGameServicesWithTestDevDb();

      var diOpts = new ServiceProviderOptions
      {
        ValidateOnBuild = true,
        ValidateScopes = true,
      };
      using var sp = services.BuildServiceProvider(diOpts);
      using var scope = sp.CreateScope();

      var db = scope.ServiceProvider.GetRequiredService<IGameDbContext>();
      using var trx = await db.BeginTransactionAsync();

      // add new team and player
      var teamSvc = scope.ServiceProvider.GetRequiredService<ITeamService>();
      var teamResult = teamSvc.CreateNewTeam("test_team");
      Assert.True(teamResult.IsSuccess);

      var playerResult = teamResult.Value.AddNewPlayer(22, "test player");
      Assert.True(playerResult.IsSuccess);

      await db.SaveChangesAsync();

      // create game
      var gameFac = scope.ServiceProvider.GetRequiredService<IGameFactory>();
      var gameResult = teamResult.Value.StartNewGame(gameFac, "Test Game", playerResult.Value);
      Assert.True(gameResult.IsSuccess);

      // spot plate
      var lpFac = scope.ServiceProvider.GetRequiredService<IGameLicensePlateFactory>();
      var spotResult = gameResult.Value.AddLicensePlateSpot(lpFac,
        new Mock<ISystemService>().Object,
        new[]
        {
          (Country.US, StateOrProvince.CA),
          (Country.US, StateOrProvince.OR),
        },
        playerResult.Value);
      Assert.True(spotResult.IsSuccess);

      await db.SaveChangesAsync();
      trx.Commit();
    }
  }
}
