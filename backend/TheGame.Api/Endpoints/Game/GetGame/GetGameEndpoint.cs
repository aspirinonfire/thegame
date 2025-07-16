using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.Game.GetGame;

public static class GetGameEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    IGameQueryProvider gameQueryProvider) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out var failure))
    {
      return failure.ToHttpResponse(ctx);
    }

    var allGames = (await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId))
      .Where(game => !game.EndedOn.HasValue)
      .ToArray();

    return Results.Ok(allGames);
  };
}
