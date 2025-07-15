using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.Game.SpotPlates;

public static class SpotPlatesEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    [FromRoute] long gameId,
    [FromBody] IReadOnlyCollection<SpottedPlate> spottedPlates,
    ICommandHandler<SpotLicensePlatesCommand, OwnedOrInvitedGame> spotPlatesHandler,
    CancellationToken cancellationToken) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out _))
    {
      return playerIdResult.ToHttpResponse(ctx);
    }

    return await spotPlatesHandler
      .Execute(
        new SpotLicensePlatesCommand(spottedPlates, gameId, playerId),
        cancellationToken)
      .ToHttpResponse(ctx);
  };
}