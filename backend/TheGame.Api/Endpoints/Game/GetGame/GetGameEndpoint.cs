using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.Game.GetGame;

public static class GetGameEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    IGameQueryProvider gameQueryProvider,
    [FromQuery(Name = "isActive")] bool? isActive) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out _))
    {
      return playerIdResult.ToHttpResponse(ctx);
    }
    
    var queryForActiveGamesOnly = isActive.GetValueOrDefault();

    var allGames = (await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId))
      .Where(game => !queryForActiveGamesOnly || !game.EndedOn.HasValue)
      .ToArray();

    return Results.Ok(allGames);
  };
}
