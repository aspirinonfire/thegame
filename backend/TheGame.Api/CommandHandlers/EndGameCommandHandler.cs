using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Api.CommandHandlers;

public sealed record EndGameCommand(long GameId, long OwnerPlayerId) : IRequest<Result<OwnedOrInvitedGame>>;

public sealed class EndGameCommandHandler(IGameDbContext gameDb,
  ITransactionExecutionWrapper transactionExecutionWrapper,
  IPlayerActionsFactory playerActionsFactory,
  ILogger<EndGameCommandHandler> logger)
    : IRequestHandler<EndGameCommand, Result<OwnedOrInvitedGame>>
{
  public async Task<Result<OwnedOrInvitedGame>> Handle(EndGameCommand request, CancellationToken cancellationToken) =>
    await transactionExecutionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(
      async () =>
      {
        var playerActions = playerActionsFactory.CreatePlayerActions(request.OwnerPlayerId);

        var endGameResult = await playerActions.EndGame(request.GameId);
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
