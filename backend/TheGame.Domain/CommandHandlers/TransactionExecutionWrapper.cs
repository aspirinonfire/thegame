using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;

namespace TheGame.Domain.CommandHandlers;

public interface ITransactionExecutionWrapper
{
  Task<Maybe<TSuccessResult>> ExecuteInTransaction<TSuccessResult>(Func<Task<Maybe<TSuccessResult>>> commandHandler,
    string commandName,
    ILogger logger,
    CancellationToken? cancellationToken = null);
}

public sealed class TransactionExecutionWrapper(IGameDbContext gameDb) : ITransactionExecutionWrapper
{
  public async Task<Maybe<TSuccessResult>> ExecuteInTransaction<TSuccessResult>(Func<Task<Maybe<TSuccessResult>>> commandHandler,
    string commandName,
    ILogger logger,
    CancellationToken? cancellationToken = default)
  {
    var cToken = cancellationToken ?? CancellationToken.None;
    logger.LogInformation("Starting transaction for {commandName}", commandName);

    using var trx = await gameDb.BeginTransactionAsync(cToken);
    var commandResult = await commandHandler();

    if (commandResult.TryGetSuccessful(out var success, out var failure))
    {
      logger.LogInformation("{commandName} was handled successfully. Committing transaction.", commandName);
      await trx.CommitAsync(cToken);
      logger.LogInformation("Transaction for {commandName} was committed successfuly.", commandName);
      return success;
    }
    else
    {
      logger.LogError("{commandName} execution returned failure. Rolling back transaction.", commandName);
      await trx.RollbackAsync(cToken);
      return failure;
    }
  }
}
