using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Teams;
using TheGame.Tests.Fixtures;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.DevTests
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
  public class GameDbContextDevTests(MsSqlFixture msSqlFixture) : IClassFixture<MsSqlFixture>
  {
    [Fact]
    public async Task CanQueryLicensePlates()
    {
      var services = CommonMockedServices.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

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
      var services = CommonMockedServices.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

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

      teamResult.AssertIsSucceessful(out var actualSuccessfulTeam);

      var playerFac = scope.ServiceProvider.GetRequiredService<IPlayerFactory>();
      var playerResult = actualSuccessfulTeam!.AddNewPlayer(playerFac, 22, "test player");
      playerResult.AssertIsSucceessful(out var actualNewPlayer);

      await db.SaveChangesAsync();

      // create game
      var gameFac = scope.ServiceProvider.GetRequiredService<IGameFactory>();
      var gameResult = actualSuccessfulTeam.StartNewGame(gameFac, "Test Game", actualNewPlayer!);
      gameResult.AssertIsSucceessful(out var actualNewGame);

      // spot plate
      var lpFac = scope.ServiceProvider.GetRequiredService<IGameLicensePlateFactory>();
      var sysService = scope.ServiceProvider.GetRequiredService<ISystemService>();
      var spotResult = actualNewGame!.AddLicensePlateSpot(lpFac,
        sysService,
        [
          (Country.US, StateOrProvince.CA),
          (Country.US, StateOrProvince.OR),
        ],
        actualNewPlayer!);

      spotResult.AssertIsSucceessful();

      await db.SaveChangesAsync();
      trx.Commit();
    }
  }
}
