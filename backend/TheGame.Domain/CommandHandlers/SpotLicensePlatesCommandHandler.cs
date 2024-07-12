using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf.Types;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.CommandHandlers;

public sealed record SpottedPlate(Country Country, StateOrProvince StateOrProvince);

public sealed record SpotLicensePlatesCommand(IReadOnlyCollection<SpottedPlate> SpottedPlates, long GameId, long SpottedByPlayerId) : IRequest<OneOf<Success, Failure>>;

public class SpotLicensePlatesCommandHandler(IGameDbContext gameDb,
  ITransactionExecutionWrapper transactionWrapper,
  IGameLicensePlateFactory gameLicensePlateFactory,
  ISystemService systemService,
  ILogger<SpotLicensePlatesCommandHandler> logger)
  : IRequestHandler<SpotLicensePlatesCommand, OneOf<Success, Failure>>
{
  public const string ActiveGameNotFoundError = "active_game_not_found";

  public async Task<OneOf<Success, Failure>> Handle(SpotLicensePlatesCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<Success>(async () =>
    {
      logger.LogInformation("Validating command");

      var activeGame = await gameDb.Games
        .Where(game => game.Id ==  request.GameId)
        .Where(game => game.CreatedByPlayerId == request.SpottedByPlayerId ||
          game.GamePlayerInvites.Any(invite => invite.PlayerId == request.SpottedByPlayerId && invite.InviteStatus == GamePlayerInviteStatus.Accepted))
        .Select(game => new
        {
          Game = game,
          Player = game.CreatedByPlayerId == request.SpottedByPlayerId ?
            game.CreatedBy : game.InvitedPlayers.First(invite => invite.Id == request.SpottedByPlayerId)
        })
        .FirstOrDefaultAsync();

      if (activeGame is null)
      {
        logger.LogError("Active game for player {playerId} not found. Execution cannot continue.", request.SpottedByPlayerId);
        return new Failure(ActiveGameNotFoundError);
      }

      var spots = request.SpottedPlates.Select(plate => (plate.Country, plate.StateOrProvince)).ToList();
      var updatedSpots = new GameLicensePlateSpots(spots, activeGame.Player);

      var updatedSpotsResult = activeGame.Game.UpdateLicensePlateSpots(gameLicensePlateFactory, systemService, updatedSpots);
      if (!updatedSpotsResult.TryGetSuccessful(out _, out var spotFailure))
      {
        logger.LogError(spotFailure.GetException(), "Failed to spot license plates.");
        return spotFailure;
      }

      await gameDb.SaveChangesAsync();
      
      logger.LogInformation("Successfully spotted license plates.");

      return new Success();

    },
    nameof(SpotLicensePlatesCommand),
    logger,
    cancellationToken);
}


