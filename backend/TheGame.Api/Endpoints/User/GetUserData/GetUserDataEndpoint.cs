using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.GetUser;

public sealed record UserData(PlayerInfo? Player, OwnedOrInvitedGame[] ActiveGames);

public static class GetUserDataEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    IPlayerService playerService,
    IGameQueryProvider gameQueryProvider,
    CancellationToken cancellationToken) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out var failure))
    {
      return failure.ToHttpResponse(ctx);
    }

    var player = await playerService
      .GetPlayerInfoQuery(playerId)
      .FirstOrDefaultAsync(cancellationToken);

    var activeGames = (await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId))
      .Where(game => !game.EndedOn.HasValue)
      .ToArray();

    return Results.Ok(new UserData(player, activeGames));
  };
}
