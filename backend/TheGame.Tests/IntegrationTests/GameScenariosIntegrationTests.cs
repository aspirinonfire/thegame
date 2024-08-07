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

namespace TheGame.Tests.IntegrationTests;

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

    var newIdentityRequest = new NewPlayerIdentityRequest("test_provider",
      "test_id",
      "Test Player",
      64,
      5);
    var newPlayerIdentityResult = playerIdentFac.CreatePlayerIdentity(newIdentityRequest);
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
    var newIdentityRequest = new NewPlayerIdentityRequest("test_provider",
      "test_id_1",
      "Test Player 1",
      64,
      5);
    var newPlayerIdentityCommandResult = await mediator.Send(new GetOrCreateNewPlayerCommand(newIdentityRequest));
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
    var newIdentityRequest = new NewPlayerIdentityRequest("test_provider",
"test_id_2",
"Test Player 2",
64,
5);
    var newPlayerIdentityCommandResult = await mediator.Send(new GetOrCreateNewPlayerCommand(newIdentityRequest));
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

  [Fact]
  public async Task CanReAddPreviouslyUnspottedPlate()
  {
    var gameId = 0L;
    var playerId = 0L;
    var connectionString = msSqlFixture.GetConnectionString();
    var services = CommonMockedServices.GetGameServicesWithTestDevDb(connectionString);

    var diOpts = new ServiceProviderOptions
    {
      ValidateOnBuild = true,
      ValidateScopes = true,
    };
    await using var sp = services.BuildServiceProvider(diOpts);
    await using (var scope = sp.CreateAsyncScope())
    {
      var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
      var gameQryProvider = scope.ServiceProvider.GetRequiredService<IGameQueryProvider>();

      // create new player with identity
      var newIdentityRequest = new NewPlayerIdentityRequest("test_provider",
"test_id_3",
"Test Player 3",
64,
5);
      var newPlayerIdentityCommandResult = await mediator.Send(new GetOrCreateNewPlayerCommand(newIdentityRequest));
      newPlayerIdentityCommandResult.AssertIsSucceessful(out var actualNewPlayerIdentity);

      // start new game
      var startNewGameCommandResult = await mediator.Send(new StartNewGameCommand("Respotted Plate Game", actualNewPlayerIdentity.PlayerId));
      startNewGameCommandResult.AssertIsSucceessful(out var actualNewGame);

      gameId = actualNewGame.GameId;
      playerId = actualNewPlayerIdentity.PlayerId;
    }

    // spot new plates +CA, +OR
    var initialSpotRequest = new SpotLicensePlatesCommand(
    [
      new SpottedPlate(Country.US, StateOrProvince.CA),
      new SpottedPlate(Country.US, StateOrProvince.OR)
    ],
    gameId,
    playerId);
    var actualInitialSpotGameResult = await RunAsScopedRequest(sp, initialSpotRequest);
    Assert.Equal(2, actualInitialSpotGameResult.GameScore.TotalScore);

    // remove one plate ~OR, +WA, +AL, -CA
    var spotRequestWithSpotRemoval = new SpotLicensePlatesCommand([
      new SpottedPlate(Country.US, StateOrProvince.OR),
      new SpottedPlate(Country.US, StateOrProvince.AL),
      new SpottedPlate(Country.US, StateOrProvince.WA)
    ],
    gameId,
    playerId);
    var actualGameAfterSpotRemoval = await RunAsScopedRequest(sp, spotRequestWithSpotRemoval);
    Assert.Equal(3, actualGameAfterSpotRemoval.GameScore.TotalScore);


    // re-add plate ~OR, ~WA, ~AL, +CA
    var spotRequestWithReAdd = new SpotLicensePlatesCommand([
      new SpottedPlate(Country.US, StateOrProvince.CA),
      new SpottedPlate(Country.US, StateOrProvince.OR),
      new SpottedPlate(Country.US, StateOrProvince.AL),
      new SpottedPlate(Country.US, StateOrProvince.WA)
    ],
    gameId,
    playerId);
    var actualGameAfterReadd = await RunAsScopedRequest(sp, spotRequestWithReAdd);
    Assert.Equal(14, actualGameAfterReadd.GameScore.TotalScore);
  }

  private async Task<T> RunAsScopedRequest<T>(IServiceProvider serviceProvider, IRequest<Result<T>> mediatorRequest)
  {
    await using var scope = serviceProvider.CreateAsyncScope();
    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

    var result = await mediator.Send(mediatorRequest);

    result.AssertIsSucceessful(out var successfulResult);

    return successfulResult;
  }
}
