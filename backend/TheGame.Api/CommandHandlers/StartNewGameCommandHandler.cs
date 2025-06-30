using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Api.CommandHandlers;

public sealed record StartNewGameCommand(string GameName, long OwnerPlayerId) : IRequest<Result<OwnedOrInvitedGame>>;

public sealed class StartNewGameCommandHandler(IGameDbContext gameDb, IPlayerActionsFactory playerActionsFactory, ITransactionExecutionWrapper transactionWrapper, ILogger<StartNewGameCommandHandler> logger)
  : IRequestHandler<StartNewGameCommand, Result<OwnedOrInvitedGame>>
{
  public async Task<Result<OwnedOrInvitedGame>> Handle(StartNewGameCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(
      async () =>
      {
        var playerActions = playerActionsFactory.CreatePlayerActions(request.OwnerPlayerId);

        var newGameResult = await playerActions.StartNewGame(request.GameName);
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