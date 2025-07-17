using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Immutable;
using System.Linq;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.Game.GetGameHistory;

public static class GetGameHistoryEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
  IGameQueryProvider gameQueryProvider) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out var failure))
    {
      return failure.ToHttpResponse(ctx);
    }

    var allEndedGames = (await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId))
      .Where(game => game.EndedOn.HasValue)
      .ToImmutableArray();

    return Results.Ok(new
    {
      NumberOfGames = allEndedGames.Length,
      SpotStats = allEndedGames
        .SelectMany(game => game.SpottedPlates)
        .GroupBy(spot => spot.Key)
        .ToDictionary(
          grp => grp.Key,
          grp => grp.Count())
    });
  };
}
