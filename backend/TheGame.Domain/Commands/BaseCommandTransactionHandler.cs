using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;

namespace TheGame.Domain.Commands
{
  public abstract class BaseCommandTransactionHandler<TCommand, TResult>(IGameDbContext gameDb) : IRequestHandler<TCommand, CommandResult<TResult>>
    where TCommand : IRequest<CommandResult<TResult>>
    where TResult : ICommandResult
  {
    public async Task<CommandResult<TResult>> Handle(TCommand request, CancellationToken cancellationToken)
    {
      using var trx = await gameDb.BeginTransactionAsync();
      var commandResult = await ExecuteCommand(request, cancellationToken);
      if (commandResult.IsSuccess)
      {
        await trx.CommitAsync(cancellationToken);
      }
      else
      {
        await trx.RollbackAsync(cancellationToken);
      }
      return commandResult;
    }

    protected abstract Task<CommandResult<TResult>> ExecuteCommand(TCommand command, CancellationToken cancellationToken);
  }
}
