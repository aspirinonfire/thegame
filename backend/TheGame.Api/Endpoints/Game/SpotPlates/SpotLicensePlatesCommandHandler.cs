using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TheGame.Api.Common;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Api.Endpoints.Game.SpotPlates;

public sealed record SpottedPlate(Country Country, StateOrProvince StateOrProvince)
{
  public string? MlPrompt { get; init; }

  public LicensePlate.PlateKey ToPlateKey() => new(Country, StateOrProvince);
}

public sealed record SpotLicensePlatesCommand(IReadOnlyCollection<SpottedPlate> SpottedPlates, long GameId, long SpottedByPlayerId);

public sealed class SpotLicensePlatesCommandHandler(ITransactionExecutionWrapper transactionWrapper,
  IGameDbContext gameDb,
  IPlayerActionsFactory playerActionsFactory,
  ILogger<SpotLicensePlatesCommandHandler> logger)
    : ICommandHandler<SpotLicensePlatesCommand, OwnedOrInvitedGame>
{
  public static string NormalizePrompt(string prompt)
  {
    if (string.IsNullOrWhiteSpace(prompt))
    {
      return string.Empty;
    }

    var trimmed = prompt.Trim();
    // Allow only English letters, digits, spaces, dashes, underscores, commas,
    // dots, question marks, exclamation marks, quotes, and apostrophes.
    // Remove everything else using an allow-list regex with timeout.
    var cleaned = System.Text.RegularExpressions.Regex.Replace(
      trimmed,
      @"[^a-zA-Z0-9 _,\.\?!'""-]+",
      string.Empty,
      System.Text.RegularExpressions.RegexOptions.None,
      System.TimeSpan.FromMilliseconds(100));

    cleaned = cleaned.Trim();

    const int MaxLen = 2048;
    if (cleaned.Length > MaxLen)
    {
      cleaned = cleaned.Substring(0, MaxLen);
    }

    return cleaned;
  }

  public async Task<Result<OwnedOrInvitedGame>> Execute(SpotLicensePlatesCommand command, CancellationToken cancellationToken) =>
    await transactionWrapper.ExecuteInTransaction<OwnedOrInvitedGame>(async () =>
    {
      var playerQuery = gameDb.Players
        .Where(player => player.Id == command.SpottedByPlayerId);

      var playerActions = playerActionsFactory.GetPlayerActions(playerQuery);

      if (playerActions == null)
      {
        return new ValidationFailure(nameof(SpotLicensePlatesCommand.SpottedByPlayerId), ErrorMessageProvider.PlayerNotFoundError);
      }

      var updatedSpots = command.SpottedPlates.Select(plate => plate.ToPlateKey()).ToList();

      var updatedSpotsResult = await playerActions.UpdateLicensePlateSpots(command.GameId, updatedSpots);
      
      if (!updatedSpotsResult.TryGetSuccessful(out var updatedGame, out var spotFailure))
      {
        logger.LogError(spotFailure.GetException(), "Failed to spot license plates.");
        return spotFailure;
      }

      // Persist ML prompt records (API concern; not in domain)
      foreach (var plateWithPrompt in command.SpottedPlates.Where(p => !string.IsNullOrWhiteSpace(p.MlPrompt)))
      {
        var plateKey = plateWithPrompt.ToPlateKey();
        var plateLookup = LicensePlate.GetLicensePlate(plateKey);
        if (!plateLookup.TryGetSuccessful(out var licensePlate, out _))
        {
          continue;
        }

        var normalizedPrompt = NormalizePrompt(plateWithPrompt.MlPrompt!);
        if (string.IsNullOrWhiteSpace(normalizedPrompt))
        {
          continue;
        }

        var mlRec = new LicensePlateSpotMlPrompt(command.GameId, command.SpottedByPlayerId, licensePlate, normalizedPrompt);
        gameDb.Add(mlRec);
      }

      await gameDb.SaveChangesAsync(cancellationToken);
      
      logger.LogInformation("Successfully spotted license plates.");

      return OwnedOrInvitedGame.FromGame(updatedGame, command.SpottedByPlayerId);
    },
    nameof(SpotLicensePlatesCommand),
    logger,
    cancellationToken);
}
