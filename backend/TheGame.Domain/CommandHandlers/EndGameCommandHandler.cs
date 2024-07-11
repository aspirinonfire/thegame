using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf.Types;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;

namespace TheGame.Domain.CommandHandlers
{
  public sealed record EndGameCommand(long GameId, long OwnerPlayerId) : IRequest<OneOf<Success, Failure>>;

  public class EndGameCommandHandler(IGameDbContext gameDb, ITransactionExecutionWrapper transactionExecutionWrapper, ISystemService systemService,  ILogger<EndGameCommandHandler> logger)
    : IRequestHandler<EndGameCommand, OneOf<Success, Failure>>
  {
    public const string ActiveGameNotFoundError = "active_game_not_found";

    public async Task<OneOf<Success, Failure>> Handle(EndGameCommand request, CancellationToken cancellationToken) =>
      await transactionExecutionWrapper.ExecuteInTransaction<Success>(
        async () =>
        {
          logger.LogInformation("Validating command");

          // TODO promote this check to a standalone "smart" query

          var ownedActiveGame = await gameDb.Games
            .Where(game => game.IsActive && game.CreatedByPlayerId == request.OwnerPlayerId)
            .FirstOrDefaultAsync();

          if (ownedActiveGame is null)
          {
            logger.LogError("Active game for player {playerId} not found. Execution cannot continue.", request.OwnerPlayerId);
            return new Failure(ActiveGameNotFoundError);
          }

          ownedActiveGame.EndGame(systemService.DateTimeOffset.UtcNow);

          await gameDb.SaveChangesAsync();

          logger.LogInformation("Game ended successully.");

          return new Success();
        },
        nameof(EndGameCommand),
        logger,
        cancellationToken);
  }
}
