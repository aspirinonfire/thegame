using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Tests.Fixtures;

namespace TheGame.Tests.IntegrationTests;

[Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
public class GameQueryProviderIntegrationTests(MsSqlFixture msSqlFixture) : IClassFixture<MsSqlFixture>
{
  [Fact]
  public async Task CanQueryOwnedAndInvitedGames()
  {
    // use raw sql to seed data for a simpler test setup: PlayerIdentity + Player + Game
    var seedSql = """
      begin transaction;
      insert into PlayerIdentities(ProviderName, ProviderIdentityId, DateCreated)
      values ('test', 'some_id', '20240101 00:00:00 +00:00');

      insert into Players ([Name], PlayerIdentityId)
      values ('Test Player', 1);

      insert into Games([Name], IsActive, CreatedByPlayerId, DateCreated, GameScore_Achievements, GameScore_TotalScore)
      values ('Test Game', 0, 1, '20240102 00:00:00 +00:00', 'West Coast;East Coast', 100);

      insert into GameLicensePlates(LicensePlateId, GameId, SpottedByPlayerId, DateCreated)
      values(1, 1, 1, '20240103 00:00:00 +00:00');
      commit;
      """;

    var services = CommonMockedServices.GetGameServicesWithTestDevDb(msSqlFixture.GetConnectionString());

    var diOpts = new ServiceProviderOptions
    {
      ValidateOnBuild = true,
      ValidateScopes = true,
    };
    using var sp = services.BuildServiceProvider(diOpts);
    using var scope = sp.CreateScope();
    
    // seed data
    var db = (GameDbContext)scope.ServiceProvider.GetRequiredService<IGameDbContext>();
    await db.Database.ExecuteSqlRawAsync(seedSql);

    var uutQueryService = scope.ServiceProvider.GetRequiredService<IGameQueryProvider>();

    var actualGames = await uutQueryService.GetOwnedAndInvitedGamesQuery(1).ToListAsync();

    var actualGame = Assert.Single(actualGames);

    Assert.Equal(1, actualGame.GameId);
    Assert.Equal("Test Game", actualGame.GameName);
    Assert.Equal(new DateTimeOffset(2024, 1, 2, 0, 0, 0, 0, TimeSpan.Zero), actualGame.DateCreated);
    Assert.True(actualGame.IsOwner);
    Assert.Collection(actualGame.GameScore.Achievements,
      achievement1 => Assert.Equal("West Coast", achievement1),
      achievement2 => Assert.Equal("East Coast", achievement2));
    Assert.Equal(100, actualGame.GameScore.TotalScore);

    var actualSpottedPlate = Assert.Single(actualGame.SpottedPlates);
    Assert.Equal(new SpottedGamePlate
    {
      Country = Domain.DomainModels.LicensePlates.Country.US,
      StateOrProvince = Domain.DomainModels.LicensePlates.StateOrProvince.AL,
      SpottedByPlayerId = 1,
      SpottedByPlayerName = "Test Player",
      SpottedOn = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero)
    },
    actualSpottedPlate);
  }
}
