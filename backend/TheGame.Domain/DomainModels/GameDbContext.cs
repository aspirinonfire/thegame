using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels;

public class GameDbContext : DbContext, IGameDbContext
{
  public const string ConnectionStringName = "GameDB";

  private readonly IMediator _mediator;
  private readonly ILogger<GameDbContext> _logger;
  private readonly ISystemService _systemService;

  public DbSet<LicensePlate> LicensePlates { get; set; } = default!;
  public DbSet<Game> Games { get; set; } = default!;
  public DbSet<Player> Players { get; set; } = default!;
  public DbSet<PlayerIdentity> PlayerIdentities { get; set; } = default!;

  // Should not be accessed directly
  // accessed from Player.Teams[].Games.Where
  //public DbSet<Game> Games { get; set; }
  // accessed from Game.LincensePlates
  //public DbSet<LicensePlateSpot> LicensePlateSpots { get; set; }

  public GameDbContext(DbContextOptions<GameDbContext> dbContextOptions,
    IMediator mediator,
    ILogger<GameDbContext> logger,
    ISystemService systemService)
    : base(dbContextOptions)
  {
    _mediator = mediator;
    _logger = logger;
    _systemService = systemService;
  }

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var dataAccessAssembly = GetType().Assembly;
    modelBuilder.ApplyConfigurationsFromAssembly(dataAccessAssembly);
    ConfigureAuditedRecords(modelBuilder.Model);
  }

  #region UoW and Domain Events
  public IDbContextTransaction BeginTransaction() => BeginTransactionAsync().GetAwaiter().GetResult();

  public async Task<IDbContextTransaction> BeginTransactionAsync() => await Database.BeginTransactionAsync();

  /// <summary>
  /// Save tracked changes
  /// </summary>
  /// <param name="acceptAllChangesOnSuccess">
  /// Handles tracked state after successful DB write:
  /// True - mark tracked entities as clean,
  /// False - keep tracked entities as updated. Useful for db error retries.
  /// </param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
    CancellationToken cancellationToken = default)
  {
    _logger.LogInformation("Saving changes.");

    var saveTime = _systemService.DateTimeOffset.Now;
    HandleAuditedRecords(saveTime);

    // this query must be ran before calling base.SaveChangesAsync() to make sure we are querying the correct state.
    var domainEvents = ChangeTracker
      .Entries()
      .Where(e => e.Entity is IDomainModel &&
        (e.State == EntityState.Added || e.State == EntityState.Modified))
      // TODO implement domain event generators to support publishing newly created ids 
      .SelectMany(e => ((IDomainModel)e.Entity).DomainEvents)
      .ToList()
      .AsReadOnly();

    var writes = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

    _logger.LogInformation("Changes saved. Will now publish {numOfDomainMessages} domain event(s).", domainEvents.Count);

    // TODO make events are only published if a) there's no transaction or b) transaction has been committed
    foreach (var e in domainEvents)
    {
      await _mediator.Publish(e, cancellationToken);
    }

    return writes;
  }

  public override int SaveChanges() =>
    SaveChangesAsync().GetAwaiter().GetResult();

  public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
    SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
    await SaveChangesAsync(true, cancellationToken);

  #endregion

  #region Record Auditing
  private void ConfigureAuditedRecords(IMutableModel model)
  {
    var datedRecordType = typeof(IAuditedRecord);

    foreach (IMutableEntityType entity in model.GetEntityTypes())
    {
      if (datedRecordType.IsAssignableFrom(entity.ClrType))
      {
        entity.AddProperty(nameof(IAuditedRecord.DateCreated), typeof(DateTimeOffset));
        entity.AddProperty(nameof(IAuditedRecord.DateModified), typeof(DateTimeOffset?));
        // TODO add modified and created by fields
      }
    }
  }

  private void HandleAuditedRecords(DateTimeOffset saveTime)
  {
    var datedRecords = ChangeTracker
      .Entries()
      .Where(e => e.Entity is IAuditedRecord &&
        (e.State == EntityState.Added || e.State == EntityState.Modified));

    foreach (var rec in datedRecords)
    {
      // TODO add created and modified by values

      rec.Property(nameof(IAuditedRecord.DateModified)).CurrentValue = saveTime;

      if (rec.State == EntityState.Added)
      {
        rec.Property(nameof(IAuditedRecord.DateCreated)).CurrentValue = saveTime;
      }
    }
  }
  #endregion
}
