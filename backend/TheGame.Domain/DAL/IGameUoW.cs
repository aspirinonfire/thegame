using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;
using System.Threading.Tasks;

namespace TheGame.Domain.DAL;

/// <summary>
/// The Game context Unit of Work
/// </summary>
public interface IGameUoW
{
  IDbContextTransaction BeginTransaction();
  Task<IDbContextTransaction> BeginTransactionAsync();
  int SaveChanges();
  int SaveChanges(bool acceptAllChangesOnSuccess);
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
  Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default);
}
