using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.Game.CreateGame;
using TheGame.Api.Endpoints.Game.EndGame;
using TheGame.Api.Endpoints.Game.SpotPlates;
using TheGame.Api.Endpoints.User;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Tests.Fixtures;

namespace TheGame.Tests.IntegrationTests;

[Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
public class GameScenariosIntegrationTests(MsSqlFixture msSqlFixture) : IClassFixture<MsSqlFixture>
{
  [Fact]
  public async Task CanCreateNewPlayer()
  {
    var services = CommonMockedServices
      .GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

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
      "Test Player");
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
    var services = CommonMockedServices
      .GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

    var diOpts = new ServiceProviderOptions
    {
      ValidateOnBuild = true,
      ValidateScopes = true,
    };
    using var sp = services.BuildServiceProvider(diOpts);

    var newPlayerIdentity = await IntegrationTestHelpers.CreatePlayerWithIdentity(sp, 
      new GetOrCreatePlayerRequest(
        new NewPlayerIdentityRequest("test_provider",
        "test_id_1",
        "Test Player 1")));

    var newGameResult = await IntegrationTestHelpers.RunAsScopedRequest<StartNewGameCommand, OwnedOrInvitedGame>(sp, 
      new StartNewGameCommand("Test Game", newPlayerIdentity.PlayerId));

    var spotResult = await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp,
      new SpotLicensePlatesCommand(
        [
          new SpottedPlate(Country.US, StateOrProvince.CA),
          new SpottedPlate(Country.US, StateOrProvince.OR),
          new SpottedPlate(Country.CA, StateOrProvince.BC)
        ],
        newGameResult.GameId,
        newPlayerIdentity.PlayerId));

    await using var scope = sp.CreateAsyncScope();
    var gameQryProvider = scope.ServiceProvider.GetRequiredService<IGameQueryProvider>();
    var actualGames = await gameQryProvider.GetOwnedAndInvitedGamesQuery(newPlayerIdentity.PlayerId);
    var actualGame = Assert.Single(actualGames);

    Assert.Equal(3, actualGame.SpottedPlates.Count);
    Assert.NotNull(actualGame.Score);
    Assert.NotEqual(0, actualGame.Score.TotalScore);
  }

  [Fact]
  public async Task WillRemoveEmptyGameFromDatabaseWhenEnding()
  {
    var services = CommonMockedServices
      .GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

    var diOpts = new ServiceProviderOptions
    {
      ValidateOnBuild = true,
      ValidateScopes = true,
    };
    using var sp = services.BuildServiceProvider(diOpts);

    // create new player with identity
    var newPlayerIdentityCommandResult = await IntegrationTestHelpers.CreatePlayerWithIdentity(sp, 
      new GetOrCreatePlayerRequest(
        new NewPlayerIdentityRequest("test_provider",
        "test_id_2",
        "Test Player 2")));

    var newGameCommandResult = await IntegrationTestHelpers.RunAsScopedRequest<StartNewGameCommand, OwnedOrInvitedGame>(sp, 
      new StartNewGameCommand("Test Game 2", newPlayerIdentityCommandResult.PlayerId));

    var actualEndGameResult = await IntegrationTestHelpers.RunAsScopedRequest<EndGameCommand, OwnedOrInvitedGame>(sp, 
      new EndGameCommand(newGameCommandResult.GameId, newPlayerIdentityCommandResult.PlayerId));

    await using var scope = sp.CreateAsyncScope();
    var gameQryProvider = scope.ServiceProvider.GetRequiredService<IGameQueryProvider>();
    var hasEmptyGame = (await gameQryProvider.GetOwnedAndInvitedGamesQuery(newPlayerIdentityCommandResult.PlayerId))
      .Where(game => game.GameId == newGameCommandResult.GameId)
      .Any();

    Assert.False(hasEmptyGame);
  }

  [Fact]
  public async Task CanReAddPreviouslyUnspottedPlate()
  {
    var gameId = 0L;
    var playerId = 0L;
    var connectionString = msSqlFixture.GetConnectionString();
    var services = CommonMockedServices
      .GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

    var diOpts = new ServiceProviderOptions
    {
      ValidateOnBuild = true,
      ValidateScopes = true,
    };
    await using var sp = services.BuildServiceProvider(diOpts);

    // create new player with identity
    var newIdentityRequest = new NewPlayerIdentityRequest("test_provider",
      "test_id_3",
      "Test Player 3");
    var newPlayerIdentityCommandResult = await IntegrationTestHelpers.CreatePlayerWithIdentity(
      sp,
      new GetOrCreatePlayerRequest(newIdentityRequest));
    playerId = newPlayerIdentityCommandResult.PlayerId;

    // start new game
    var startNewGameCommandResult = await IntegrationTestHelpers.RunAsScopedRequest<StartNewGameCommand, OwnedOrInvitedGame>(
      sp,
      new StartNewGameCommand("Respotted Plate Game", playerId));
    gameId = startNewGameCommandResult.GameId;

    // spot new plates +CA, +OR
    var initialSpotRequest = new SpotLicensePlatesCommand(
      [
        new SpottedPlate(Country.US, StateOrProvince.CA),
        new SpottedPlate(Country.US, StateOrProvince.OR)
      ],
      gameId,
      playerId);
    var actualInitialSpotGameResult = await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp, initialSpotRequest);
    Assert.Equal(2, actualInitialSpotGameResult.Score.TotalScore);

    // remove one plate ~OR, +WA, +AL, -CA
    var spotRequestWithSpotRemoval = new SpotLicensePlatesCommand(
      [
        new SpottedPlate(Country.US, StateOrProvince.OR),
        new SpottedPlate(Country.US, StateOrProvince.AL),
        new SpottedPlate(Country.US, StateOrProvince.WA)
      ],
      gameId,
      playerId);
    var actualGameAfterSpotRemoval = await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp, spotRequestWithSpotRemoval);
    Assert.Equal(3, actualGameAfterSpotRemoval.Score.TotalScore);

    // re-add plate ~OR, ~WA, ~AL, +CA
    var spotRequestWithReAdd = new SpotLicensePlatesCommand(
      [
        new SpottedPlate(Country.US, StateOrProvince.CA),
        new SpottedPlate(Country.US, StateOrProvince.OR),
        new SpottedPlate(Country.US, StateOrProvince.AL),
        new SpottedPlate(Country.US, StateOrProvince.WA)
      ],
      gameId,
      playerId);
    
    var actualGameAfterReadd = await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp, spotRequestWithReAdd);
    Assert.Equal(14, actualGameAfterReadd.Score.TotalScore);
  }

  // helpers moved to TestUtils
}
