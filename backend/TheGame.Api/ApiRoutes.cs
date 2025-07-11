using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TheGame.Api.Auth;
using TheGame.Api.CommandHandlers;
using TheGame.Api.Common;

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
      // extract Player ID from claims for easy access. If playerId claim is not present, short-circuit with BadRequest
      .AddEndpointFilter(async (invocationContext, next) =>
      {
        // skip reading player id from claims only for anonymous endpoints
        var currentEndpoint = invocationContext.HttpContext.GetEndpoint();
        if (currentEndpoint?.Metadata.Any(mt => mt is IAllowAnonymous) ?? false)
        {
          return await next(invocationContext);
        }

        var playerIdClaim = invocationContext.HttpContext.User.Claims
          .FirstOrDefault(claim => claim.Type == GameAuthService.PlayerIdClaimType);

        if (playerIdClaim?.Value == null || !long.TryParse(playerIdClaim.Value, out var playerId))
        {
          throw new InvalidOperationException(InvalidPlayerIdClaimError);
        }

        invocationContext.HttpContext.Items[PlayerIdItemKey] = playerId;

        return await next(invocationContext);
      });

    apiRoute
      .MapPost("/user/google/apitoken", async (HttpContext ctx, GameAuthService authService, [FromBody] string credential) =>
      {
        return await authService
          .AuthenticateWithGoogleAuthCode(credential, ctx)
          .ToHttpResponse(ctx);
      })
      .WithDescription("Validate Google Auth Code and generate API tokens for accessing Game APIs.")
      .AllowAnonymous();

    apiRoute
      .MapPost("/user/refresh-token", async (HttpContext ctx, GameAuthService authService) =>
      {
        // we cannot use GetTokenAsync because expired tokens are not saved in context
        var accessToken = ctx.Request.Headers.Authorization
          .FirstOrDefault()?
          .Split(" ")
          .LastOrDefault() ?? string.Empty;

        return await authService
          .RefreshAccessToken(ctx, accessToken)
          .ToHttpResponse(ctx);
      })
      .WithDescription("Refresh Game API Token using Refresh Cookie")
      .AllowAnonymous();
    
    apiRoute
      .MapGet("user", async (HttpContext ctx, IPlayerQueryProvider playerQueryProvider) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);

        var player = await playerQueryProvider.GetPlayerInfoQuery(playerId).FirstOrDefaultAsync();

        return Results.Ok(player);
      })
      .WithDescription("Get user details for authenticated player.");


    apiRoute
      .MapGet("game", async (HttpContext ctx, IGameQueryProvider gameQueryProvider, [FromQuery(Name ="isActive")] bool? isActive) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);
        var queryForActiveGamesOnly = isActive.GetValueOrDefault();

        var allGames = (await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId))
          .Where(game => !queryForActiveGamesOnly || !game.EndedOn.HasValue)
          .ToArray();
      
        return Results.Ok(allGames);
      })
      .WithDescription("Retrieve all games for an authenticated player.");

    
    apiRoute
      .MapPost("game",
        async (HttpContext ctx,
          [FromBody] StartNewGameRequest newGameRequest,
          ICommandHandler<StartNewGameCommand, OwnedOrInvitedGame> startGameHandler,
          CancellationToken cancellationToken) =>
        {
          var playerId = GetPlayerIdFromHttpContext(ctx);

          return await startGameHandler
            .Execute(
              new StartNewGameCommand(newGameRequest.NewGameName, playerId),
              cancellationToken)
            .ToHttpResponse(ctx);
        })
        .WithDescription("Start new game for an authenticated player.");


    apiRoute
      .MapPost("game/{gameId:long}/endgame",
        async (HttpContext ctx,
          [FromRoute] long gameId,
          ICommandHandler<EndGameCommand, OwnedOrInvitedGame> endGameHandler,
          CancellationToken cancellationToken) =>
        {
          var playerId = GetPlayerIdFromHttpContext(ctx);
          return await endGameHandler
            .Execute(
              new EndGameCommand(gameId, playerId),
              cancellationToken)
            .ToHttpResponse(ctx);
        })
        .WithDescription("End active game for an authenticated player.");

 
    apiRoute
      .MapPost("game/{gameId:long}/spotplates",
        async (HttpContext ctx,
          [FromRoute] long gameId,
          [FromBody] IReadOnlyCollection<SpottedPlate> spottedPlates,
          ICommandHandler<SpotLicensePlatesCommand, OwnedOrInvitedGame> spotPlatesHandler,
          CancellationToken cancellationToken) =>
        {
          var playerId = GetPlayerIdFromHttpContext(ctx);

          return await spotPlatesHandler
            .Execute(
              new SpotLicensePlatesCommand(spottedPlates, gameId, playerId),
              cancellationToken)
            .ToHttpResponse(ctx);
        })
        .WithDescription("Updated spotted license plates for an active game.");

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
