using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TheGame.Domain.DAL
{
  /// <summary>
  /// DB transaction that can trigger custom Task after successful commit
  /// </summary>
  public class GameTransaction : IDbContextTransaction
  {
    private readonly IDbContextTransaction _trx;
    private readonly Func<Task> _onCommit;

    public Guid TransactionId => _trx.TransactionId;

    public GameTransaction(IDbContextTransaction trx, Func<Task> onCommit)
    {
      _trx = trx;
      _onCommit = onCommit;
    }

    public void Commit()
    {
      CommitAsync().GetAwaiter().GetResult();
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
      await _trx.CommitAsync(cancellationToken);
      if (_onCommit == null)
      {
        return;
      }
      await _onCommit.Invoke();
    }

    public void Dispose()
    {
      _trx.Dispose();
    }

    public ValueTask DisposeAsync()
    {
      return _trx.DisposeAsync();
    }

    public void Rollback()
    {
      _trx.Rollback();
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
      return _trx.RollbackAsync(cancellationToken);
    }
  }
}
