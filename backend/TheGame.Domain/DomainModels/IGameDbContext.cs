using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels;

public interface IGameDbContext
{
  DbSet<Player> Players { get; }
  DbSet<PlayerIdentity> PlayerIdentities { get; }

  EntityEntry Add(object entity);

  IDbContextTransaction BeginTransaction();
  Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
  int SaveChanges();
  int SaveChanges(bool acceptAllChangesOnSuccess);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
  Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
}
