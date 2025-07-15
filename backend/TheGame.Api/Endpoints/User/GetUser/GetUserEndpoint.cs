using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.GetUser;

public static class GetUserEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx, IPlayerQueryProvider playerQueryProvider) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out _))
    {
      return playerIdResult.ToHttpResponse(ctx);
    }

    var player = await playerQueryProvider
      .GetPlayerInfoQuery(playerId)
      .FirstOrDefaultAsync();

    return Results.Ok(player);
  };
}
