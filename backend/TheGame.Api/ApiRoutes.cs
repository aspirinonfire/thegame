using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TheGame.Api.Auth;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.Utils;

namespace TheGame.Api;

public sealed record StartNewGameRequest(string NewGameName);

public static class ApiRoutes
{
  public const string PlayerIdItemKey = "PlayerId";
  public const string InvalidPlayerIdClaimError = "invalid_player_id_claim";

  public static IEndpointRouteBuilder AddGameApiRoutes(this IEndpointRouteBuilder endpoints)
  {
    var apiRoute = endpoints
      .MapGroup("api")
      .AddEndpointFilter(async (invocationContext, next) =>
      {
        var playerIdClaim = invocationContext.HttpContext.User.Claims
          .FirstOrDefault(claim => claim.Type == GameAuthService.PlayerIdClaimType);

        if (playerIdClaim?.Value == null || !long.TryParse(playerIdClaim.Value, out var playerId))
        {
          return Results.BadRequest(InvalidPlayerIdClaimError);
        }

        invocationContext.HttpContext.Items[PlayerIdItemKey] = playerId;

        return await next(invocationContext);
      });

    
    apiRoute.MapGet("user", async (HttpContext ctx, IPlayerQueryProvider playerQueryProvider) =>
    {
      var playerId = GetPlayerIdFromHttpContext(ctx);

      var player = await playerQueryProvider.GetPlayerInfoQuery(playerId).FirstOrDefaultAsync();

      return Results.Ok(player);
    });

    
    apiRoute.MapGet("game", async (HttpContext ctx, IGameQueryProvider gameQueryProvider) =>
    {
      var playerId = GetPlayerIdFromHttpContext(ctx);

      var allGames = await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId).ToListAsync();
      
      return Results.Ok(allGames);
    });

    
    apiRoute.MapPost("game", async (HttpContext ctx, IMediator mediator, [FromBody] StartNewGameRequest newGameRequest) =>
    {
      var playerId = GetPlayerIdFromHttpContext(ctx);
      
      var newGameResult = await mediator.Send(new StartNewGameCommand(newGameRequest.NewGameName, playerId));
      if (!newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
      {
        return Results.BadRequest(newGameFailure.ErrorMessage);
      }

      return Results.Ok(newGame);

    });

    return endpoints;
  }

  private static long GetPlayerIdFromHttpContext(HttpContext httpContext)
  {
    var playerIdValue = httpContext.Items[PlayerIdItemKey];

    if (playerIdValue is long playerId)
    {
      return playerId;
    }

    throw new InvalidOperationException("PlayerId stored in http context is not a number!");

  }
}
