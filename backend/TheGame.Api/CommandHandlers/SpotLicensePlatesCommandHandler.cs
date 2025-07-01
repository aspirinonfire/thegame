using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Api.CommandHandlers;

public sealed record SpottedPlate(Country Country, StateOrProvince StateOrProvince)
{
  public LicensePlate.PlateKey ToPlateKey() => new(Country, StateOrProvince);
}

public sealed record SpotLicensePlatesCommand(IReadOnlyCollection<SpottedPlate> SpottedPlates, long GameId, long SpottedByPlayerId) 
  : IRequest<Result<OwnedOrInvitedGame>>;

public sealed class SpotLicensePlatesCommandHandler(ITransactionExecutionWrapper transactionWrapper,
  IGameDbContext gameDb,
  IPlayerActionsFactory playerActionsFactory,
  ILogger<SpotLicensePlatesCommandHandler> logger)
  : IRequestHandler<SpotLicensePlatesCommand, Result<OwnedOrInvitedGame>>
{
  public async Task<Result<OwnedOrInvitedGame>> Handle(SpotLicensePlatesCommand request, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(async () =>
    {
      var playerQuery = gameDb.Players
        .Where(player => player.Id == request.SpottedByPlayerId);

      var playerActions = playerActionsFactory.GetPlayerActions(playerQuery);

      if (playerActions == null)
      {
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
      }

      var updatedSpots = request.SpottedPlates.Select(plate => plate.ToPlateKey()).ToList();

      var updatedSpotsResult = await playerActions.UpdateLicensePlateSpots(request.GameId, updatedSpots);
      
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


