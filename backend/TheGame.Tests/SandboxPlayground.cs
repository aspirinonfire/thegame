using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ValueGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using System.Linq.Expressions;

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
      var services = new ServiceCollection();
      
      AddTestDb(services);

      using var serviceProvider = services.BuildServiceProvider();
      using var scope = serviceProvider.CreateScope();

      var dbContext = scope.ServiceProvider.GetRequiredService<SandboxTestDbContext>();
      dbContext.Database.EnsureCreated();

      var newGame = new SandboxTestGame(new SandboxTestGame.NewGameData("Test Game"));
      dbContext.Add(newGame);

      await dbContext.SaveChangesAsync();
      dbContext.ChangeTracker.Clear();

      var addedGame = await dbContext.GamesForUpdates.FirstOrDefaultAsync(game => game.GameId == newGame.GameId);

      Assert.NotNull(addedGame);

      addedGame.UpdateSpots([
        new SandboxTestGame.NewSpotData("US", "California", DateTimeOffset.UtcNow),
        new SandboxTestGame.NewSpotData("US", "Texas", DateTimeOffset.UtcNow)
      ]);

      await dbContext.SaveChangesAsync();
      dbContext.ChangeTracker.Clear();

      var queriedGame = await dbContext.GamesReadonly
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
    }

    public class SandboxTestDbContext(DbContextOptions<SandboxTestDbContext> options) : DbContext(options)
    {
      public IQueryable<IGame> GamesReadonly => Games.AsQueryable().AsNoTracking();
      public IQueryable<SandboxTestGame> GamesForUpdates => Games
        .Include(game => game.Spots);
      
      public IQueryable<SandboxTestSpot> SpotsReadonly => Spots.AsQueryable().AsNoTracking();

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
            .WithOne()
            .HasForeignKey(s => s.ParentGameId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

          game.Navigation(e => e.Spots)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

          ConfigureRowVersionColumn(game, g => g.RowVersion);
        });

        modelBuilder.Entity<SandboxTestSpot>(spot =>
        {
          spot.HasKey(s => new { s.ParentGameId, s.Country, s.StateOrProvince });
          spot.Property(s => s.SpottedOn).IsRequired();

          ConfigureRowVersionColumn(spot, s => s.RowVersion);
        });
      }

      private void ConfigureRowVersionColumn<T>(EntityTypeBuilder<T> entityBuilder, Expression<Func<T, byte[]>> rowVersionPropSelector)
        where T: class
      {
        var rowVersionProp = entityBuilder.Property(rowVersionPropSelector);

        if (Database.IsInMemory())
        {
          rowVersionProp
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate()
            .HasValueGenerator<InMemoryRowVersionGenerator>();
        }
        else
        {
          rowVersionProp.IsRowVersion();
        }
      }

      internal sealed class InMemoryRowVersionGenerator : ValueGenerator<byte[]>
      {
        private static long _counter = DateTime.UtcNow.Ticks;

        public override bool GeneratesTemporaryValues => false;

        public override byte[] Next(EntityEntry entry)
        {
          var next = Interlocked.Increment(ref _counter);
          return BitConverter.GetBytes(next);  // 8-byte little-endian
        }
      }
    }

    public interface IGame
    {
      DateTimeOffset CreatedOn { get; }
      DateTimeOffset? EndedOn { get; }
      long GameId { get; }
      string Name { get; }
      IReadOnlySet<SandboxTestSpot> Spots { get; }
      byte[] RowVersion { get; }
    }

    public class SandboxTestGame : IGame
    {
      public long GameId { get; protected set; }

      public string Name { get; protected set; } = default!;

      protected HashSet<SandboxTestSpot> _spots = [];
      public IReadOnlySet<SandboxTestSpot> Spots => _spots;

      public DateTimeOffset CreatedOn { get; protected set; } = DateTimeOffset.UtcNow;

      public DateTimeOffset? EndedOn { get; protected set; }

      public byte[] RowVersion { get; protected set; } = default!;

      protected SandboxTestGame() { }

      internal SandboxTestGame(NewGameData newGame) : this()
      {
        ArgumentException.ThrowIfNullOrEmpty(newGame?.Name);

        Name = newGame.Name;
        CreatedOn = DateTimeOffset.UtcNow;
      }

      public void UpdateSpots(IReadOnlyCollection<NewSpotData> newSpots)
      {
        ArgumentNullException.ThrowIfNull(newSpots);

        if (EndedOn.HasValue)
        {
          throw new InvalidOperationException("Game has already ended.");
        }

        var existingSpotKeys = _spots
          .Select(s => (s.Country, s.StateOrProvince))
          .ToHashSet();

        var newSpotKeys = newSpots.Select(ns => (ns.Country, ns.StateOrProvince)).ToHashSet();

        var toRemove = _spots
          .Where(spot => !newSpotKeys.Contains((spot.Country, spot.StateOrProvince)))
          .ToImmutableArray();
        foreach (var spot in toRemove)
        {
          _spots.Remove(spot);
        }

        _spots.RemoveWhere(_spots => !newSpotKeys.Contains((_spots.Country, _spots.StateOrProvince)));

        _spots.UnionWith(newSpots
          .Where(newSpot => !existingSpotKeys.Contains((newSpot.Country, newSpot.StateOrProvince)))
          .Select(newSpot => new SandboxTestSpot(newSpot.ToNewSpot(GameId))));
      }

      public void EndGame()
      {
        if (EndedOn.HasValue)
        {
          throw new InvalidOperationException("Game has already ended.");
        }

        EndedOn = DateTimeOffset.UtcNow;
      }

      public sealed record NewSpotData(string Country, string StateOrProvince, DateTimeOffset SpottedOn)
      {
        internal SandboxTestSpot.NewSpot ToNewSpot(long gameId) => new(gameId, Country, StateOrProvince, SpottedOn);
      }

      public sealed record NewGameData(string Name);
    }

    public class SandboxTestSpot
    {
      public string Country { get; protected set; } = default!;
      public string StateOrProvince { get; protected set; } = default!;
      public DateTimeOffset SpottedOn { get; protected set; } = DateTimeOffset.UtcNow;
      public long ParentGameId { get; protected set; }

      public byte[] RowVersion { get; protected set; } = default!;

      protected SandboxTestSpot() { }

      internal SandboxTestSpot(NewSpot newSpot)
      {
        ArgumentNullException.ThrowIfNull(newSpot);

        ParentGameId = newSpot.GameId;
        Country = newSpot.Country;
        StateOrProvince = newSpot.StateOrProvince;
        SpottedOn = newSpot.SpottedOn;
      }

      internal sealed record NewSpot(long GameId, string Country, string StateOrProvince, DateTimeOffset SpottedOn);
    }
  }
}
