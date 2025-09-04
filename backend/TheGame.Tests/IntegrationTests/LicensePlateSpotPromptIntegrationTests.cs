using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.Game.CreateGame;
using TheGame.Api.Endpoints.Game.SpotPlates;
using TheGame.Api.Endpoints.User;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Tests.Fixtures;

namespace TheGame.Tests.IntegrationTests;

[Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
public class LicensePlateSpotPromptIntegrationTests(MsSqlFixture msSqlFixture) : IClassFixture<MsSqlFixture>
{
  [Fact]
  public async Task CreateSpot_WithPrompt_PersistsPromptRecord()
  {
    var services = CommonMockedServices.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());
    await using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    var player = await IntegrationTestHelpers.CreatePlayerWithIdentity(sp, new GetOrCreatePlayerRequest(new NewPlayerIdentityRequest("prov","id_prompt_1","Player Prompt 1")));
    var game = await IntegrationTestHelpers.RunAsScopedRequest<StartNewGameCommand, OwnedOrInvitedGame>(sp, new StartNewGameCommand("Prompt Game", player.PlayerId));

    var cmd = new SpotLicensePlatesCommand([
      new SpottedPlate(Country.US, StateOrProvince.CA) { MlPrompt = "detect sedan california" },
    ], game.GameId, player.PlayerId);

    await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp, cmd);

    await using var scope = sp.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var rec = await db.Set<LicensePlateSpotMlPrompt>().AsNoTracking().FirstOrDefaultAsync(r => r.GameId == game.GameId);
    Assert.NotNull(rec);
    Assert.Equal(game.GameId, rec!.GameId);
    Assert.Equal(player.PlayerId, rec.SpottedByPlayerId);
    Assert.Equal(LicensePlate.LicensePlatesByCountryAndProvinceLookup[new LicensePlate.PlateKey(Country.US, StateOrProvince.CA)].Id, rec.LicensePlateId);
    Assert.Equal("detect sedan california", rec.MlPrompt);
  }

  [Fact]
  public async Task CreateSpot_WithoutPrompt_Succeeds_NoPromptRecord()
  {
    var services = CommonMockedServices.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());
    await using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    var player = await IntegrationTestHelpers.CreatePlayerWithIdentity(sp, new GetOrCreatePlayerRequest(new NewPlayerIdentityRequest("prov","id_prompt_2","Player Prompt 2")));
    var game = await IntegrationTestHelpers.RunAsScopedRequest<StartNewGameCommand, OwnedOrInvitedGame>(sp, new StartNewGameCommand("No Prompt Game", player.PlayerId));

    var cmd = new SpotLicensePlatesCommand([
      new SpottedPlate(Country.US, StateOrProvince.OR),
      new SpottedPlate(Country.CA, StateOrProvince.BC)
    ], game.GameId, player.PlayerId);

    var result = await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp, cmd);
    Assert.True(result.Score.TotalScore >= 2);

    await using var scope = sp.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var anyPromptRec = await db.Set<LicensePlateSpotMlPrompt>().AsNoTracking().AnyAsync(r => r.GameId == game.GameId);
    Assert.False(anyPromptRec);
  }

  [Fact]
  public async Task CreateSpot_WithPrompt_ThenRemoveSpot_OperationSucceeds_PromptNotDuplicated()
  {
    var services = CommonMockedServices.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());
    await using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

    var player = await IntegrationTestHelpers.CreatePlayerWithIdentity(sp, new GetOrCreatePlayerRequest(new NewPlayerIdentityRequest("prov","id_prompt_3","Player Prompt 3")));
    var game = await IntegrationTestHelpers.RunAsScopedRequest<StartNewGameCommand, OwnedOrInvitedGame>(sp, new StartNewGameCommand("Remove Prompt Game", player.PlayerId));

    // initial spot with prompt
    var initial = new SpotLicensePlatesCommand([
      new SpottedPlate(Country.US, StateOrProvince.WA) { MlPrompt = "mt rainier washington" },
      new SpottedPlate(Country.US, StateOrProvince.OR)
    ], game.GameId, player.PlayerId);
    await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp, initial);

    // remove WA by not including it
    var removal = new SpotLicensePlatesCommand([
      new SpottedPlate(Country.US, StateOrProvince.OR)
    ], game.GameId, player.PlayerId);
    var afterRemoval = await IntegrationTestHelpers.RunAsScopedRequest<SpotLicensePlatesCommand, OwnedOrInvitedGame>(sp, removal);
    Assert.True(afterRemoval.Score.TotalScore >= 1);

    await using var scope = sp.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    var promptRecs = await db.Set<LicensePlateSpotMlPrompt>().AsNoTracking().Where(r => r.GameId == game.GameId).ToListAsync();
    // Only the initial prompt record should exist; no new record on removal
    Assert.Single(promptRecs);
  }
}
