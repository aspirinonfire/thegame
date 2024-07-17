using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels;

public class GameDbContext(DbContextOptions<GameDbContext> dbContextOptions,
  IMediator mediator,
  ILogger<GameDbContext> logger,
  ISystemService systemService) : DbContext(dbContextOptions), IGameDbContext
{
  public const string ConnectionStringName = "GameDB";

  public DbSet<LicensePlate> LicensePlates { get; set; } = default!;
  public DbSet<Game> Games { get; set; } = default!;
  public DbSet<Player> Players { get; set; } = default!;
  public DbSet<PlayerIdentity> PlayerIdentities { get; set; } = default!;

  private readonly Queue<IDomainEvent> _domainEventsToPublish = [];

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    var dataAccessAssembly = GetType().Assembly;
    modelBuilder.ApplyConfigurationsFromAssembly(dataAccessAssembly);
    ConfigureAuditedRecords(modelBuilder.Model);
  }

  #region Transaction Handlers
  public IDbContextTransaction BeginTransaction() => BeginTransactionAsync().GetAwaiter().GetResult();

  public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
  {
    var underlyingTransaction = await Database.BeginTransactionAsync(cancellationToken);
    return new GameDbTransactionWrapper(underlyingTransaction, PublishDomainEvents);
  }

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
    var saveTime = systemService.DateTimeOffset.Now;
    HandleAuditedRecords(saveTime);

    // this query must be ran before calling base.SaveChangesAsync() to make sure we are querying the correct state.
    var domainEvents = ChangeTracker
      .Entries()
      .Where(e => e.Entity is IDomainModel &&
        (e.State == EntityState.Added || e.State == EntityState.Modified))
      .SelectMany(e => ((IDomainModel)e.Entity).DomainEvents)
      .ToList()
      .AsReadOnly();

    var writes = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

    foreach (var domainEvent in domainEvents)
    {
      _domainEventsToPublish.Enqueue(domainEvent);
    }

    if (Database.CurrentTransaction == null)
    {
      await PublishDomainEvents(cancellationToken);
    }

    return writes;
  }

  public override int SaveChanges() => SaveChangesAsync().GetAwaiter().GetResult();

  public override int SaveChanges(bool acceptAllChangesOnSuccess) => SaveChangesAsync(acceptAllChangesOnSuccess).GetAwaiter().GetResult();

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => await SaveChangesAsync(true, cancellationToken);

  private async Task PublishDomainEvents(CancellationToken cancellationToken)
  {
    logger.LogInformation("Changes saved. Will now publish {numOfEvents} domain event(s).", _domainEventsToPublish.Count);

    while (_domainEventsToPublish.Count > 0)
    {
      var domainEvent = _domainEventsToPublish.Dequeue();
      await mediator.Publish(domainEvent, cancellationToken);
    }
  }
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
      }
    }
  }

  private void HandleAuditedRecords(DateTimeOffset saveTime)
  {
    var datedRecords = ChangeTracker
      .Entries<IAuditedRecord>()
      .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
      .ToList();

    foreach (var rec in datedRecords)
    {
      // using Reflection because these properties are not writable otherwise
      if (rec.State == EntityState.Modified)
      {
        rec.Property(nameof(IAuditedRecord.DateModified)).CurrentValue = saveTime;
      }

      if (rec.State == EntityState.Added)
      {
        rec.Property(nameof(IAuditedRecord.DateCreated)).CurrentValue = saveTime;
      }
    }
  }
  #endregion

  private sealed class GameDbTransactionWrapper(IDbContextTransaction underlyingTransaction, Func<CancellationToken, Task> onCommit) : IDbContextTransaction
  {
    public readonly List<IDomainEvent> ToPublish = [];

    public Guid TransactionId => underlyingTransaction.TransactionId;

    public void Commit() => CommitAsync().GetAwaiter().GetResult();

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
      await underlyingTransaction.CommitAsync(cancellationToken);
      await onCommit(cancellationToken);
    }

    public void Dispose() => underlyingTransaction.Dispose();

    public ValueTask DisposeAsync() => underlyingTransaction.DisposeAsync();

    public void Rollback() => underlyingTransaction.Rollback();

    public Task RollbackAsync(CancellationToken cancellationToken = default) => underlyingTransaction.RollbackAsync(cancellationToken);
  }
}
