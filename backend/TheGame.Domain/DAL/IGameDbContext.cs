using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.DAL
{
  public interface IGameDbContext
  {
    DbSet<LicensePlateModel> LicensePlates { get; set; }

    IDbContextTransaction BeginTransaction();
    Task<IDbContextTransaction> BeginTransactionAsync();
    int SaveChanges();
    int SaveChanges(bool acceptAllChangesOnSuccess);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);

    EntityEntry<T> Entry<T>(T entity) where T : class;
    EntityEntry Add([NotNull] object entity);
  }
}
