using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;

namespace TheGame.Domain.CommandHandlers
{
  public interface ITransactionExecutionWrapper
  {
    Task<OneOf<TSuccessResult, Failure>> ExecuteInTransaction<TSuccessResult>(Func<Task<OneOf<TSuccessResult, Failure>>> commandHandler,
      string commandName,
      ILogger logger,
      CancellationToken? cancellationToken = null);
  }

  public sealed class TransactionExecutionWrapper(IGameDbContext gameDb) : ITransactionExecutionWrapper
  {
    public async Task<OneOf<TSuccessResult, Failure>> ExecuteInTransaction<TSuccessResult>(Func<Task<OneOf<TSuccessResult, Failure>>> commandHandler,
      string commandName,
      ILogger logger,
      CancellationToken? cancellationToken = default)
    {
      logger.LogInformation("Starting transaction for {commandName}", commandName);

      using var trx = await gameDb.BeginTransactionAsync();
      var commandResult = await commandHandler();

      if (commandResult.TryGetSuccessful(out var success, out var failure))
      {
        logger.LogInformation("{commandName} was handled successfully. Committing transaction.", commandName);
        await trx.CommitAsync(cancellationToken ?? CancellationToken.None);
        logger.LogInformation("Transaction for {commandName} was committed successfuly.", commandName);
        return success;
      }
      else
      {
        logger.LogError("{commandName} execution returned failure. Rolling back transaction.", commandName);
        await trx.RollbackAsync(cancellationToken ?? CancellationToken.None);
        return failure;
      }
    }
  }
}
