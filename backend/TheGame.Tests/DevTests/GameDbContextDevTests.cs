using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Teams;
using TheGame.Domain.Utils;
using TheGame.Tests.Fixtures;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.DevTests
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.DevTest)]
  public class GameDbContextDevTests(MsSqlFixture msSqlFixture) : IClassFixture<MsSqlFixture>
  {
    [Fact]
    public async Task CanQueryLicensePlates()
    {
      var services = DevTestFixture.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

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
      var services = DevTestFixture.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

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

      var playerFac = scope.ServiceProvider.GetRequiredService<IPlayerFactory>();
      var playerResult = teamResult.Value!.AddNewPlayer(playerFac, 22, "test player");
      Assert.True(playerResult.IsSuccess);

      await db.SaveChangesAsync();

      // create game
      var gameFac = scope.ServiceProvider.GetRequiredService<IGameFactory>();
      var gameResult = teamResult.Value.StartNewGame(gameFac, "Test Game", playerResult.Value!);
      Assert.True(gameResult.IsSuccess);

      // spot plate
      var lpFac = scope.ServiceProvider.GetRequiredService<IGameLicensePlateFactory>();
      var sysService = scope.ServiceProvider.GetRequiredService<ISystemService>();
      var spotResult = gameResult.Value!.AddLicensePlateSpot(lpFac,
        sysService,
        [
          (Country.US, StateOrProvince.CA),
          (Country.US, StateOrProvince.OR),
        ],
        playerResult.Value!);

      Assert.True(spotResult.IsSuccess);

      await db.SaveChangesAsync();
      trx.Commit();
    }
  }
}
