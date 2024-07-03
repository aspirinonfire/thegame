using MediatR;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;

namespace TheGame.Domain.CommandHandlers;

public sealed record StartNewGameCommand(string GameName, long OwnerPlayerId) : IRequest<OneOf<StartNewGameResult, Failure>>;

public sealed record StartNewGameResult(long GameId);

public class StartNewGameCommandHandler(IGameDbContext gameDb, IGameFactory gameFactory, ITransactionExecutionWrapper transactionWrapper)
  : IRequestHandler<StartNewGameCommand, OneOf<StartNewGameResult, Failure>>
{
  public const string PlayerNotFoundError = "player_not_found";

  public async Task<OneOf<StartNewGameResult, Failure>> Handle(StartNewGameCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<StartNewGameResult>(
      async () =>
      {
        var player = await gameDb.Players
          .FindAsync(request.OwnerPlayerId);

        if (player is null)
        {
          return new Failure(PlayerNotFoundError);
        }

        var newGameResult = await gameFactory.StartNewGame(request.GameName, player);
        if (!newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
        {
          return newGameFailure;
        }

        await gameDb.SaveChangesAsync();
        
        return new StartNewGameResult(newGame.Id);
      },
      cancellationToken);
}