using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Tests.Fixtures;

namespace TheGame.Tests.IntegrationTests
{
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

        insert into Games([Name], IsActive, CreatedByPlayerId, DateCreated)
        values ('Test Game', 0, 1, '20240102 00:00:00 +00:00');

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

      Assert.Equal(
        new OwnedAndInvitedGames
        {
          IsOwner = true,
          GameId = 1,
          GameName = "Test Game",
          DateCreated = new DateTimeOffset(2024, 1, 2, 0, 0, 0, 0, TimeSpan.Zero),
          DateModified = null,
          EndedOn = null,
          NumberOfSpottedPlates = 1
        },
        actualGame);
    }
  }
}
