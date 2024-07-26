using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;

namespace TheGame.Domain.CommandHandlers;

public sealed record EndGameCommand(long GameId, long OwnerPlayerId) : IRequest<Maybe<OwnedOrInvitedGame>>;

public sealed class EndGameCommandHandler(IGameDbContext gameDb, ITransactionExecutionWrapper transactionExecutionWrapper, ILogger<EndGameCommandHandler> logger)
  : IRequestHandler<EndGameCommand, Maybe<OwnedOrInvitedGame>>
{
  public async Task<Maybe<OwnedOrInvitedGame>> Handle(EndGameCommand request, CancellationToken cancellationToken) =>
    await transactionExecutionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(
      async () =>
      {
        logger.LogInformation("Validating command");

        var ownedActiveGame = await gameDb.Games
          .Where(game => game.IsActive && game.CreatedByPlayerId == request.OwnerPlayerId)
          .FirstOrDefaultAsync(cancellationToken);

        if (ownedActiveGame is null)
        {
          logger.LogError("Active game for player {playerId} not found. Execution cannot continue.", request.OwnerPlayerId);
          return new Failure(ErrorMessageProvider.ActiveGameNotFoundError);
        }

        // if there are no plates, remove the game altogether
        if (ownedActiveGame.GameLicensePlates.Count == 0)
        {
          gameDb.Games.Remove(ownedActiveGame);
          await gameDb.SaveChangesAsync(cancellationToken);
          return new OwnedOrInvitedGame();
        }

        var endGameResult = ownedActiveGame.EndGame();
        if (!endGameResult.TryGetSuccessful(out var endedGame, out var failure))
        {
          logger.LogError(failure.GetException(), "Failed to end game.");
          return failure;
        }

        await gameDb.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Game ended successully.");

        return OwnedOrInvitedGame.FromGame(endedGame, request.OwnerPlayerId);
      },
      nameof(EndGameCommand),
      logger,
      cancellationToken);
}
