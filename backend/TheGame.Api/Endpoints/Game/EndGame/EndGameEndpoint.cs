using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.Game.EndGame;

public static class EndGameEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    [FromRoute] long gameId,
    ICommandHandler<EndGameCommand, OwnedOrInvitedGame> endGameHandler,
    CancellationToken cancellationToken) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out var failure))
    {
      return failure.ToHttpResponse(ctx);
    }

    return await endGameHandler
      .Execute(
        new EndGameCommand(gameId, playerId),
        cancellationToken)
      .ToHttpResponse(ctx);
  };
}