using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.Game.CreateGame;

public sealed record StartNewGameRequest(string NewGameName);

public static class CreateGameEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    [FromBody] StartNewGameRequest newGameRequest,
    ICommandHandler<StartNewGameCommand, OwnedOrInvitedGame> startGameHandler,
    CancellationToken cancellationToken) =>
  {
    var playerIdResult = ctx.GetPlayerIdFromHttpContext();
    if (!playerIdResult.TryGetSuccessful(out var playerId, out _))
    {
      return playerIdResult.ToHttpResponse(ctx);
    }

    return await startGameHandler
      .Execute(
        new StartNewGameCommand(newGameRequest.NewGameName, playerId),
        cancellationToken)
      .ToHttpResponse(ctx);
  };
}
