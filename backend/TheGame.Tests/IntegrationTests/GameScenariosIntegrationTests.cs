using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Tests.Fixtures;

namespace TheGame.Tests.IntegrationTests
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
  public class GameScenariosIntegrationTests(MsSqlFixture msSqlFixture) : IClassFixture<MsSqlFixture>
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
    public async Task CanCreateNewPlayer()
    {
      var services = CommonMockedServices
        .GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString())
        .AddLogging(provider => provider.AddDebug());

      var diOpts = new ServiceProviderOptions
      {
        ValidateOnBuild = true,
        ValidateScopes = true,
      };
      using var sp = services.BuildServiceProvider(diOpts);
      using var scope = sp.CreateScope();

      var db = scope.ServiceProvider.GetRequiredService<IGameDbContext>();
      var playerIdentFac = scope.ServiceProvider.GetRequiredService<IPlayerIdentityFactory>();

      var newPlayerIdentityResult = playerIdentFac.CreatePlayerIdentity(new NewPlayerIdentityRequest("test_provider", "test_id", "refresh_token", "Test Player"));
      newPlayerIdentityResult.AssertIsSucceessful();

      await db.SaveChangesAsync();

      var actualPlayerIdentity = await db.PlayerIdentities
        .AsNoTracking()
        .Include(ident => ident.Player)
        .FirstOrDefaultAsync();

      Assert.NotEqual(0, actualPlayerIdentity?.Player?.Id);
    }

    [Fact]
    public async Task CanCreatePlayerStartNewGameAndSpotPlates()
    {
      var services = CommonMockedServices.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

      var diOpts = new ServiceProviderOptions
      {
        ValidateOnBuild = true,
        ValidateScopes = true,
      };
      using var sp = services.BuildServiceProvider(diOpts);
      using var scope = sp.CreateScope();

      var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

      // create new player with identity
      var newPlayerRequest = new NewPlayerIdentityRequest("test_provider_1", "test_id_1", "refresh_token", "Test Player");
      var newPlayerIdentityCommandResult = await mediator.Send(new GetOrCreateNewPlayerCommand(newPlayerRequest));
      newPlayerIdentityCommandResult.AssertIsSucceessful(out var actualNewPlayerIdentity);

      // start new game
      var startNewGameCommandResult = await mediator.Send(new StartNewGameCommand("Test Game", actualNewPlayerIdentity.PlayerId));
      startNewGameCommandResult.AssertIsSucceessful(out var newGame);

      // TODO replace with commands
      var lpFac = scope.ServiceProvider.GetRequiredService<IGameLicensePlateFactory>();
      var sysService = scope.ServiceProvider.GetRequiredService<ISystemService>();
      var db = scope.ServiceProvider.GetRequiredService<IGameDbContext>();
      using var trx = await db.BeginTransactionAsync();

      var actualNewGame = await db.Games.FindAsync(newGame.GameId);
      Assert.NotNull(actualNewGame);

      // spot plate
      var newSpots = new GameLicensePlateSpots(
        [
          (Country.US, StateOrProvince.CA),
          (Country.US, StateOrProvince.OR),
        ],
        DateTimeOffset.UtcNow,
        actualNewGame.CreatedBy);

      var spotResult = actualNewGame.UpdateLicensePlateSpots(lpFac,
        sysService,
        newSpots);

      spotResult.AssertIsSucceessful(actualGame =>
      {
        Assert.Equal(2, actualNewGame.GameLicensePlates.Count);
        Assert.All(actualGame.GameLicensePlates,
          plate => Assert.Equal(actualNewGame.CreatedBy, plate.SpottedBy));
      });

      await db.SaveChangesAsync();
      trx.Commit();
    }
  }
}
