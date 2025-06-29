using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.Utils;

namespace TheGame.Api.CommandHandlers;

public sealed record StartNewGameCommand(string GameName, long OwnerPlayerId) : IRequest<Result<OwnedOrInvitedGame>>;

public sealed class StartNewGameCommandHandler(IGameDbContext gameDb, IGameFactory gameFactory, ITransactionExecutionWrapper transactionWrapper, ILogger<StartNewGameCommandHandler> logger)
  : IRequestHandler<StartNewGameCommand, Result<OwnedOrInvitedGame>>
{
  public async Task<Result<OwnedOrInvitedGame>> Handle(StartNewGameCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(
      async () =>
      {
        logger.LogInformation("Validating command...");

        var player = await gameDb.Players.FindAsync(request.OwnerPlayerId, cancellationToken);

        if (player is null)
        {
          logger.LogError("Player identity not found. Execution cannot continue.");
          return new Failure(ErrorMessageProvider.PlayerNotFoundError);
        }
        
        logger.LogInformation("Command is valid. Attempting to create new game.");

        var newGameResult = await gameFactory.StartNewGame(request.GameName, player);
        if (!newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
        {
          logger.LogError(newGameFailure.GetException(), "New game cannot be started.");
          return newGameFailure;
        }

        await gameDb.SaveChangesAsync(cancellationToken);

        logger.LogInformation("New game started successully.");

        return OwnedOrInvitedGame.FromGame(newGame, request.OwnerPlayerId);
      },
      nameof(StartNewGameCommand),
      logger,
      cancellationToken);
}