using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;
using System.Threading;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain.DomainModels;

public interface IGameDbContext
{
  DbSet<LicensePlate> LicensePlates { get; }
  DbSet<Player> Players { get; }
  DbSet<Game> Games { get; }
  DbSet<PlayerIdentity> PlayerIdentities { get; }

  EntityEntry<T> Entry<T>(T entity) where T : class;
  EntityEntry Add(object entity);

  IDbContextTransaction BeginTransaction();
  Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
  int SaveChanges();
  int SaveChanges(bool acceptAllChangesOnSuccess);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
  Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
}
