using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.GetUser;

public static class GetUserEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx, IPlayerService playerService, CancellationToken cancellationToken) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out _))
    {
      return playerIdResult.ToHttpResponse(ctx);
    }

    var player = await playerService
      .GetPlayerInfoQuery(playerId)
      .FirstOrDefaultAsync(cancellationToken);

    return Results.Ok(player);
  };
}
