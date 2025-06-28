using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TheGame.Tests
{
  /// <summary>
  /// These tests are intended for trying things out in a sandbox environment.
  /// </summary>
  [Trait(XunitTestProvider.Category, XunitTestProvider.DevTest)]
  public class SandboxPlayground
  {
    [Fact]
    public async Task WillHandleInvariantsGracefully()
    {
      var services = new ServiceCollection()
        .AddScoped<IGameRepository, GameRepository>();
      
      AddTestDb(services);

      using var serviceProvider = services.BuildServiceProvider();
      using var scope = serviceProvider.CreateScope();

      var gameRepository = scope.ServiceProvider.GetRequiredService<IGameRepository>();
      
      var dbContext = scope.ServiceProvider.GetRequiredService<SandboxTestDbContext>();
      dbContext.Database.EnsureCreated();

      var newGame = new SandboxTestGame(new SandboxTestGame.NewGame("Test Game"));
      dbContext.Games.Add(newGame);

      await dbContext.SaveChangesAsync();

      var addedGame = await gameRepository.GetGameByIdAsync(1);

      Assert.NotNull(addedGame);

      addedGame.UpdateSpots([
        new SandboxTestGame.NewSpot("US", "California", DateTimeOffset.UtcNow),
        new SandboxTestGame.NewSpot("US", "Texas", DateTimeOffset.UtcNow)
      ]);

      await dbContext.SaveChangesAsync();

      var queriedGame = await dbContext.GamesQuery
        .Include(game => game.Spots)
        .ToListAsync();

      var actualGame = Assert.Single(queriedGame);
      Assert.Equal(2, actualGame.Spots.Count);
    }

    private static void AddTestDb(IServiceCollection services)
    {
      services
        .AddDbContext<SandboxTestDbContext>(options =>
        {
          options.UseInMemoryDatabase("SandboxTestDb");

          options.UseLoggerFactory(
            LoggerFactory.Create(builder => builder.AddDebug()));

          options.EnableSensitiveDataLogging(true);
        });

      services.AddScoped<IQueryableContext>(isp => isp.GetRequiredService<SandboxTestDbContext>());
    }

    public interface IQueryableContext
    {
      IQueryable<IGame> GamesQuery { get; }
      IQueryable<ISpot> SpotsQuery { get; }
    }

    public class SandboxTestDbContext(DbContextOptions<SandboxTestDbContext> options) : DbContext(options), IQueryableContext
    {
      public IQueryable<IGame> GamesQuery => Games.AsQueryable().AsNoTracking();
      public IQueryable<ISpot> SpotsQuery => Spots.AsQueryable().AsNoTracking();

      internal DbSet<SandboxTestGame> Games { get; set; } = default!;
      internal DbSet<SandboxTestSpot> Spots { get; set; } = default!;

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        modelBuilder.Entity<SandboxTestGame>(game =>
        {
          game.HasKey(g => g.GameId);
          game.Property(g => g.Name).IsRequired();
          game
            .HasMany(g => g.Spots)
            .WithOne(s => s.ParentGame)
            .HasForeignKey(s => s.ParentGameId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

          game.Navigation(e => e.Spots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SandboxTestSpot>(spot =>
        {
          spot.HasKey(s => new { s.ParentGameId, s.Country, s.StateOrProvince });
          spot.Property(s => s.SpottedOn).IsRequired();
        });
      }
    }

    public interface IGame
    {
      DateTimeOffset CreatedOn { get; }
      DateTimeOffset? EndedOn { get; }
      long GameId { get; }
      string Name { get; }
      IReadOnlySet<SandboxTestSpot> Spots { get; }
    }

    public interface IGameMutable : IGame
    {
      void EndGame();
      void UpdateSpots(IReadOnlyCollection<SandboxTestGame.NewSpot> spots);
    }

    public class SandboxTestGame : IGameMutable
    {
      public long GameId { get; protected set; }

      public string Name { get; protected set; } = default!;

      protected HashSet<SandboxTestSpot> _spots = [];
      public IReadOnlySet<SandboxTestSpot> Spots => _spots;

      public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.UtcNow;

      public DateTimeOffset? EndedOn { get; protected set; }

      protected SandboxTestGame() { }

      public SandboxTestGame(NewGame newGame) : this()
      {
        ArgumentException.ThrowIfNullOrEmpty(newGame?.Name);

        Name = newGame.Name;
        CreatedOn = DateTimeOffset.UtcNow;
      }

      public void UpdateSpots(IReadOnlyCollection<NewSpot> spots)
      {
        ArgumentNullException.ThrowIfNull(spots);

        if (EndedOn.HasValue)
        {
          throw new InvalidOperationException("Game has already ended.");
        }

        var existingSpots = _spots
          .Select(s => (s.Country, s.StateOrProvince))
          .ToHashSet();

        var newSpotKeys = spots.Select(ns => (ns.Country, ns.StateOrProvince)).ToHashSet();

        _spots.RemoveWhere(s => !newSpotKeys.Contains((s.Country, s.StateOrProvince)));

        _spots.UnionWith(
          spots
            .Where(s => !existingSpots.Contains((s.Country, s.StateOrProvince)))
            .Select(s => new SandboxTestSpot(
              new SandboxTestSpot.NewSpot(GameId, s.Country, s.StateOrProvince, s.SpottedOn))));
      }

      public void EndGame()
      {
        if (EndedOn.HasValue)
        {
          throw new InvalidOperationException("Game has already ended.");
        }

        EndedOn = DateTimeOffset.UtcNow;
      }

      public sealed record NewSpot(string Country, string StateOrProvince, DateTimeOffset SpottedOn);

      public sealed record NewGame(string Name);
    }

    public interface ISpot
    {
      string Country { get; }
      SandboxTestGame ParentGame { get; }
      long ParentGameId { get; }
      DateTimeOffset SpottedOn { get; }
      string StateOrProvince { get; }
    }

    public class SandboxTestSpot : ISpot
    {
      public string Country { get; protected set; } = default!;
      public string StateOrProvince { get; protected set; } = default!;
      public DateTimeOffset SpottedOn { get; protected set; } = DateTimeOffset.UtcNow;

      public long ParentGameId { get; protected set; }
      public SandboxTestGame ParentGame { get; protected set; } = default!;

      protected SandboxTestSpot() { }

      public SandboxTestSpot(NewSpot newSpot)
      {
        ArgumentNullException.ThrowIfNull(newSpot);

        ParentGameId = newSpot.gameId;
        Country = newSpot.Country;
        StateOrProvince = newSpot.StateOrProvince;
        SpottedOn = newSpot.SpottedOn;
      }

      public sealed record NewSpot(long gameId, string Country, string StateOrProvince, DateTimeOffset SpottedOn);
    }

    public interface IGameRepository
    {
      Task<IGameMutable?> GetGameByIdAsync(long gameId);
    }

    public sealed class GameRepository(SandboxTestDbContext dbContext) : IGameRepository
    {
      public async Task<IGameMutable?> GetGameByIdAsync(long gameId)
      {
        return await dbContext.Games
          .Include(game => game.Spots)
          .FirstOrDefaultAsync(g => g.GameId == gameId);
      }
    }
  }
}
