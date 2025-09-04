using System;
using TheGame.Domain.DomainModels.Common;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.LicensePlates;

public class LicensePlateSpotMlPrompt : IAuditedRecord
{
  public long Id { get; protected set; }

  public long GameId { get; protected set; }
  public Game Game { get; protected set; } = default!;

  public long SpottedByPlayerId { get; protected set; }
  public Player SpottedBy { get; protected set; } = default!;

  public long LicensePlateId { get; protected set; }
  public LicensePlate LicensePlate { get; protected set; } = default!;

  public string MlPrompt { get; protected set; } = string.Empty;

  public DateTimeOffset DateCreated { get; }
  public DateTimeOffset? DateModified { get; }

  protected LicensePlateSpotMlPrompt() { }

  public LicensePlateSpotMlPrompt(long gameId, long spottedByPlayerId, LicensePlate licensePlate, string mlPrompt)
  {
    ArgumentNullException.ThrowIfNull(licensePlate);
    ArgumentException.ThrowIfNullOrWhiteSpace(mlPrompt);

    GameId = gameId;
    SpottedByPlayerId = spottedByPlayerId;
    LicensePlate = licensePlate;
    LicensePlateId = licensePlate.Id;
    MlPrompt = mlPrompt;
  }
}
