using System;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;

namespace TheGame.Domain.CommandHandlers
{
  public interface ITransactionExecutionWrapper
  {
    Task<OneOf<TSuccessResult, Failure>> ExecuteInTransaction<TSuccessResult>(Func<Task<OneOf<TSuccessResult, Failure>>> commandHandler, CancellationToken? cancellationToken = null);
  }

  public sealed class TransactionExecutionWrapper(IGameDbContext gameDb) : ITransactionExecutionWrapper
  {
    public async Task<OneOf<TSuccessResult, Failure>> ExecuteInTransaction<TSuccessResult>(Func<Task<OneOf<TSuccessResult, Failure>>> commandHandler,
      CancellationToken? cancellationToken = default)
    {
      using var trx = await gameDb.BeginTransactionAsync();
      var commandResult = await commandHandler();

      if (commandResult.TryGetSuccessful(out var success, out var failure))
      {
        await trx.CommitAsync(cancellationToken ?? CancellationToken.None);
        return success;
      }
      else
      {
        await trx.RollbackAsync(cancellationToken ?? CancellationToken.None);
        return failure;
      }
    }
  }
}
