using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.Utils;

namespace TheGame.Api;

/// <summary>
/// Command Handler
/// </summary>
/// <typeparam name="TCommand"></typeparam>
/// <typeparam name="TCommandResult"></typeparam>
public interface ICommandHandler<TCommand, TCommandResult>
    where TCommand : class
    where TCommandResult : class
{
  /// <summary>
  /// Execute commmand
  /// </summary>
  /// <param name="command"></param>
  /// <param name="cancellationToken"></param>
  /// <returns></returns>
  Task<Result<TCommandResult>> Execute(TCommand command, CancellationToken cancellationToken);
}