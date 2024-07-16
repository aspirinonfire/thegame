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
      var gameQryProvider = scope.ServiceProvider.GetRequiredService<IGameQueryProvider>();

      // create new player with identity
      var newPlayerRequest = new NewPlayerIdentityRequest("test_provider_1", "test_id_1", "refresh_token", "Test Player");
      var newPlayerIdentityCommandResult = await mediator.Send(new GetOrCreateNewPlayerCommand(newPlayerRequest));
      newPlayerIdentityCommandResult.AssertIsSucceessful(out var actualNewPlayerIdentity);

      // start new game
      var startNewGameCommandResult = await mediator.Send(new StartNewGameCommand("Test Game", actualNewPlayerIdentity.PlayerId));
      startNewGameCommandResult.AssertIsSucceessful(out var actualNewGame);

      // spot plates
      var spotPlatesResult = await mediator.Send(new SpotLicensePlatesCommand([
        new SpottedPlate(Country.US, StateOrProvince.CA),
        new SpottedPlate(Country.US, StateOrProvince.OR),
        new SpottedPlate(Country.CA, StateOrProvince.BC)
        ],
        actualNewGame.GameId,
        actualNewPlayerIdentity.PlayerId));

      spotPlatesResult.AssertIsSucceessful();

      var actualGames = await gameQryProvider.GetOwnedAndInvitedGamesQuery(actualNewPlayerIdentity.PlayerId).ToListAsync();
      var actualGame = Assert.Single(actualGames);

      Assert.Equal(3, actualGame.SpottedPlates.Count);
      Assert.NotNull(actualGame.GameScore);
      Assert.NotEqual(0, actualGame.GameScore.TotalScore);
    }

    [Fact]
    public async Task WillRemoveEmptyGameFromDatabaseWhenEnding()
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
      var gameQryProvider = scope.ServiceProvider.GetRequiredService<IGameQueryProvider>();

      // create new player with identity
      var newPlayerRequest = new NewPlayerIdentityRequest("test_provider_2", "test_id_2", "refresh_token", "Test Player 2");
      var newPlayerIdentityCommandResult = await mediator.Send(new GetOrCreateNewPlayerCommand(newPlayerRequest));
      newPlayerIdentityCommandResult.AssertIsSucceessful(out var actualNewPlayerIdentity);

      // start new game
      var startNewGameCommandResult = await mediator.Send(new StartNewGameCommand("Empty Game", actualNewPlayerIdentity.PlayerId));
      startNewGameCommandResult.AssertIsSucceessful(out var actualNewGame);

      // end newly created game
      var actualEndGameResult = await mediator.Send(new EndGameCommand(actualNewGame.GameId, actualNewPlayerIdentity.PlayerId));
      actualEndGameResult.AssertIsSucceessful();

      var hasEmptyGame = await gameQryProvider.GetOwnedAndInvitedGamesQuery(actualNewPlayerIdentity.PlayerId)
        .Where(game => game.GameId == actualNewGame.GameId)
        .AnyAsync();

      Assert.False(hasEmptyGame);
    }
  }
}
