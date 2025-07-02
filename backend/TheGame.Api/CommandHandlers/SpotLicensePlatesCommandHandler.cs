using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Api.CommandHandlers;

// TODO move to endpoint
public sealed record SpottedPlate(Country Country, StateOrProvince StateOrProvince)
{
  public LicensePlate.PlateKey ToPlateKey() => new(Country, StateOrProvince);
}

public sealed record SpotLicensePlatesCommand(IReadOnlyCollection<SpottedPlate> SpottedPlates, long GameId, long SpottedByPlayerId);

public sealed class SpotLicensePlatesCommandHandler(ITransactionExecutionWrapper transactionWrapper,
  IGameDbContext gameDb,
  IPlayerActionsFactory playerActionsFactory,
  ILogger<SpotLicensePlatesCommandHandler> logger)
    : ICommandHandler<SpotLicensePlatesCommand, OwnedOrInvitedGame>
{
  public async Task<Result<OwnedOrInvitedGame>> Execute(SpotLicensePlatesCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(async () =>
    {
      var playerQuery = gameDb.Players
        .Where(player => player.Id == command.SpottedByPlayerId);

      var playerActions = playerActionsFactory.GetPlayerActions(playerQuery);

      if (playerActions == null)
      {
        return new Failure(ErrorMessageProvider.PlayerNotFoundError);
      }

      var updatedSpots = command.SpottedPlates.Select(plate => plate.ToPlateKey()).ToList();

      var updatedSpotsResult = await playerActions.UpdateLicensePlateSpots(command.GameId, updatedSpots);
      
      if (!updatedSpotsResult.TryGetSuccessful(out var updatedGame, out var spotFailure))
      {
        logger.LogError(spotFailure.GetException(), "Failed to spot license plates.");
        return spotFailure;
      }

      await gameDb.SaveChangesAsync(cancellationToken);
      
      logger.LogInformation("Successfully spotted license plates.");

      return OwnedOrInvitedGame.FromGame(updatedGame, command.SpottedByPlayerId);
    },
    nameof(SpotLicensePlatesCommand),
    logger,
    cancellationToken);
}


