using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Users;
using TheGame.Tests.Fixtures;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests.IntegrationTests
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
  public class GameDbContextIntegrationTests(MsSqlFixture msSqlFixture) : IClassFixture<MsSqlFixture>
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
      var playerIdentFac = scope.ServiceProvider.GetRequiredService<IPlayerIdentityFactory>();
      
      var gameFac = scope.ServiceProvider.GetRequiredService<IGameFactory>();
      var lpFac = scope.ServiceProvider.GetRequiredService<IGameLicensePlateFactory>();
      var sysService = scope.ServiceProvider.GetRequiredService<ISystemService>();

      using var trx = await db.BeginTransactionAsync();

      var newPlayerIdentityResult = playerIdentFac.CreatePlayerIdentity(new NewPlayerIdentityRequest("test_provider", "test_id", "refresh_token", "Test Player"));
      newPlayerIdentityResult.AssertIsSucceessful(out var actualNewPlayerIdentity,
        identity =>
        {
          Assert.NotNull(identity.Player);
        });

      await db.SaveChangesAsync();

      // create game
      var gameResult = gameFac.CreateNewGame("Test Game", actualNewPlayerIdentity.Player!);
      gameResult.AssertIsSucceessful(out var actualNewGame);

      await db.SaveChangesAsync();

      // spot plate
      var spotResult = actualNewGame.AddLicensePlateSpot(lpFac,
        sysService,
        [
          (Country.US, StateOrProvince.CA),
          (Country.US, StateOrProvince.OR),
        ],
        actualNewPlayerIdentity.Player!);

      spotResult.AssertIsSucceessful(actualGame =>
      {
        Assert.Equal(2, actualNewGame.GameLicensePlates.Count);
        Assert.All(actualGame.GameLicensePlates,
          plate => Assert.Equal(actualNewPlayerIdentity.Player, plate.SpottedBy));
      });

      await db.SaveChangesAsync();
      trx.Commit();
    }
  }
}
