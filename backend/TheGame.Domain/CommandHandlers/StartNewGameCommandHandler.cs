using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;

namespace TheGame.Domain.CommandHandlers;

public sealed record StartNewGameCommand(string GameName, long OwnerPlayerId) : IRequest<OneOf<OwnedOrInvitedGame, Failure>>;

public class StartNewGameCommandHandler(IGameDbContext gameDb, IGameFactory gameFactory, ITransactionExecutionWrapper transactionWrapper, ILogger<StartNewGameCommandHandler> logger)
  : IRequestHandler<StartNewGameCommand, OneOf<OwnedOrInvitedGame, Failure>>
{
  public const string PlayerNotFoundError = "player_not_found";

  public async Task<OneOf<OwnedOrInvitedGame, Failure>> Handle(StartNewGameCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(
      async () =>
      {
        logger.LogInformation("Validating command...");

        var player = await gameDb.Players
          .FindAsync(request.OwnerPlayerId);

        if (player is null)
        {
          logger.LogError("Player identity not found. Execution cannot continue.");
          return new Failure(PlayerNotFoundError);
        }
        
        logger.LogInformation("Command is valid. Attempting to create new game.");

        var newGameResult = await gameFactory.StartNewGame(request.GameName, player);
        if (!newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
        {
          logger.LogError(newGameFailure.GetException(), "New game cannot be started.");
          return newGameFailure;
        }

        await gameDb.SaveChangesAsync();

        logger.LogInformation("New game started successully.");

        return OwnedOrInvitedGame.FromGame(newGame, request.OwnerPlayerId);
      },
      nameof(StartNewGameCommand),
      logger,
      cancellationToken);
}