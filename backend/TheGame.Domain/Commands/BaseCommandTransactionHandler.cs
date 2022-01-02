using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DAL;

namespace TheGame.Domain.Commands
{
  public abstract class BaseCommandTransactionHandler<TCommand, TResult> : IRequestHandler<TCommand, CommandResult<TResult>>
    where TCommand : IRequest<CommandResult<TResult>>
    where TResult : ICommandResult
  {
    private readonly IGameUoW _gameUoW;

    protected BaseCommandTransactionHandler(IGameUoW gameUoW)
    {
      _gameUoW = gameUoW;
    }

    public async Task<CommandResult<TResult>> Handle(TCommand request, CancellationToken cancellationToken)
    {
      using var trx = await _gameUoW.BeginTransactionAsync();
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
