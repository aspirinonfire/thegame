using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Api.Common;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Api.Endpoints.Game.CreateGame;

public sealed record StartNewGameCommand(string GameName, long OwnerPlayerId);

public sealed class StartNewGameCommandHandler(IGameDbContext gameDb,
  IPlayerActionsFactory playerActionsFactory,
  ITransactionExecutionWrapper transactionWrapper, ILogger<StartNewGameCommandHandler> logger)
    : ICommandHandler<StartNewGameCommand, OwnedOrInvitedGame>
{
  public async Task<Result<OwnedOrInvitedGame>> Execute(StartNewGameCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(
      async () =>
      {
        var playerQuery = gameDb.Players
          .Where(player => player.Id == command.OwnerPlayerId);

        var playerActions = playerActionsFactory.GetPlayerActions(playerQuery);

        if (playerActions == null)
        {
          return new ValidationFailure(nameof(StartNewGameCommand.OwnerPlayerId), ErrorMessageProvider.PlayerNotFoundError);
        }

        var newGameResult = await playerActions.StartNewGame(command.GameName);
        if (!newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
        {
          logger.LogError(newGameFailure.GetException(), "New game cannot be started.");
          return newGameFailure;
        }

        await gameDb.SaveChangesAsync(cancellationToken);

        logger.LogInformation("New game started successully.");

        return OwnedOrInvitedGame.FromGame(newGame, command.OwnerPlayerId);
      },
      nameof(StartNewGameCommand),
      logger,
      cancellationToken);
}