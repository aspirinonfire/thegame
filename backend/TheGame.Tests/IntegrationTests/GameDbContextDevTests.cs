using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Teams;
using TheGame.Tests.Fixtures;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.IntegrationTests
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
    public async Task CanCreateTeamPlayerGameAndAddSpot()
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
      var teamSvc = scope.ServiceProvider.GetRequiredService<ITeamService>();

      using var trx = await db.BeginTransactionAsync();

      var actualNewTeamResult = await teamSvc.CreateNewTeam("test_team");
      actualNewTeamResult.AssertIsSucceessful(out var actualSuccessfulTeam);

      var actualNewPlayerResult = teamSvc.AddNewPlayer(actualSuccessfulTeam, 22, "test player");
      actualNewPlayerResult.AssertIsSucceessful(out var actualNewPlayer);

      await db.SaveChangesAsync();

      // create game
      var actualNewGameResult = teamSvc.StartNewGame(actualSuccessfulTeam, "Test Game", actualNewPlayer);
      actualNewGameResult.AssertIsSucceessful(out var actualNewGame);

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

      spotResult.AssertIsSucceessful(actualGame =>
      {
        Assert.Equal(2, actualNewGame.GameLicensePlates.Count);

        Assert.All(actualGame.GameLicensePlates,
          plate => Assert.Equal(actualNewPlayer, plate.SpottedBy));
      });

      await db.SaveChangesAsync();
      trx.Commit();
    }
  }
}
