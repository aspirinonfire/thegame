using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;

namespace TheGame.Domain.CommandHandlers;

public sealed record SpottedPlate(Country Country, StateOrProvince StateOrProvince)
{
  public LicensePlate.PlateKey ToPlateKey() => new(Country, StateOrProvince);
}

public sealed record SpotLicensePlatesCommand(IReadOnlyCollection<SpottedPlate> SpottedPlates, long GameId, long SpottedByPlayerId) 
  : IRequest<Maybe<OwnedOrInvitedGame>>;

public sealed class SpotLicensePlatesCommandHandler(IGameDbContext gameDb,
  ITransactionExecutionWrapper transactionWrapper,
  IGameLicensePlateFactory gameLicensePlateFactory,
  ISystemService systemService,
  IGameScoreCalculator gameScoreCalculator,
  ILogger<SpotLicensePlatesCommandHandler> logger)
  : IRequestHandler<SpotLicensePlatesCommand, Maybe<OwnedOrInvitedGame>>
{
  public async Task<Maybe<OwnedOrInvitedGame>> Handle(SpotLicensePlatesCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(async () =>
    {
      logger.LogInformation("Validating command");

      var activeGame = await gameDb.Games
        .Include(game => game.InvitedPlayers)
        .Include(game => game.GameLicensePlates)
        .Where(game => game.Id ==  request.GameId)
        .Where(game => game.CreatedByPlayerId == request.SpottedByPlayerId ||
          game.GamePlayerInvites.Any(invite => invite.PlayerId == request.SpottedByPlayerId && invite.InviteStatus == GamePlayerInviteStatus.Accepted))
        .Select(game => new
        {
          Game = game,
          Player = game.CreatedByPlayerId == request.SpottedByPlayerId ?
            game.CreatedBy : game.InvitedPlayers.First(invite => invite.Id == request.SpottedByPlayerId)
        })
        .FirstOrDefaultAsync(cancellationToken);

      if (activeGame is null)
      {
        logger.LogError("Active game for player {playerId} not found. Execution cannot continue.", request.SpottedByPlayerId);
        return new Failure(ErrorMessageProvider.ActiveGameNotFoundError);
      }

      var spots = request.SpottedPlates.Select(plate => plate.ToPlateKey()).ToList();
      var updatedSpots = new GameLicensePlateSpots(spots, activeGame.Player);

      var updatedSpotsResult = activeGame.Game.UpdateLicensePlateSpots(gameLicensePlateFactory,
        systemService,
        gameScoreCalculator,
        gameDb,
        updatedSpots);
      
      if (!updatedSpotsResult.TryGetSuccessful(out var updatedGame, out var spotFailure))
      {
        logger.LogError(spotFailure.GetException(), "Failed to spot license plates.");
        return spotFailure;
      }

      await gameDb.SaveChangesAsync(cancellationToken);
      
      logger.LogInformation("Successfully spotted license plates.");

      return OwnedOrInvitedGame.FromGame(updatedGame, request.SpottedByPlayerId);
    },
    nameof(SpotLicensePlatesCommand),
    logger,
    cancellationToken);
}


