using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using TheGame.Api.Auth;
using TheGame.Api.CommandHandlers;
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
          return Results.BadRequest(InvalidPlayerIdClaimError);
        }

        invocationContext.HttpContext.Items[PlayerIdItemKey] = playerId;

        return await next(invocationContext);
      });

    apiRoute
      .MapPost("/user/google/apitoken", async (HttpContext ctx, GameAuthService authService, [FromBody] string credential) =>
      {
        var apiTokenResult = await authService.AuthenticateWithGoogleAuthCode(credential, ctx);
        if (!apiTokenResult.TryGetSuccessful(out var apiTokens, out var tokenFailure))
        {
          return Results.BadRequest(tokenFailure.ErrorMessage);
        }

        return Results.Ok(new
        {
          apiTokens.AccessToken
        });
      })
      .WithDescription("Validate Google ID Token and generate API tokens for accessing Game APIs.")
      .AllowAnonymous();

    apiRoute
      .MapPost("/user/refresh-token", async (HttpContext ctx, GameAuthService authService) =>
      {
        // we cannot use GetTokenAsync because expired tokens are not saved in context
        var accessToken = ctx.Request.Headers.Authorization
          .FirstOrDefault()?
          .Split(" ")
          .LastOrDefault() ?? string.Empty;

        var refreshResult = await authService.RefreshAccessToken(ctx, accessToken);
        if (!refreshResult.TryGetSuccessful(out var refreshTokens, out var tokenFailure))
        {
          return Results.BadRequest(tokenFailure.ErrorMessage);
        }

        return Results.Ok(new
        {
          refreshTokens.AccessToken
        });
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

        var allGames = await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId)
          .Where(game => !queryForActiveGamesOnly || !game.EndedOn.HasValue)
          .ToListAsync();
      
        return Results.Ok(allGames);
      })
      .WithDescription("Retrieve all games for an authenticated player.");

    
    apiRoute
      .MapPost("game", async (HttpContext ctx, IMediator mediator, [FromBody] StartNewGameRequest newGameRequest) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);
      
        var newGameResult = await mediator.Send(new StartNewGameCommand(newGameRequest.NewGameName, playerId));
        if (!newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
        {
          return Results.BadRequest(newGameFailure.ErrorMessage);
        }

        return Results.Ok(newGame);
      })
      .WithDescription("Start new game for an authenticated player.");

    
    apiRoute
      .MapPost("game/{gameId:long}/endgame", async (HttpContext ctx, IMediator mediator, [FromRoute] long gameId) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);
        var endGameResult = await mediator.Send(new EndGameCommand(gameId, playerId));
        if (!endGameResult.TryGetSuccessful(out var endedGame, out var endGameFailure))
        {
          return Results.BadRequest(endGameFailure.ErrorMessage);
        }

        return Results.Ok(endedGame);
      })
      .WithDescription("End active game for an authenticated player.");

    
    apiRoute
      .MapPost("game/{gameId:long}/spotplates", async (HttpContext ctx, IMediator mediator, [FromRoute] long gameId, [FromBody] IReadOnlyCollection<SpottedPlate> spottedPlates) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);

        var endGameResult = await mediator.Send(new SpotLicensePlatesCommand(spottedPlates, gameId, playerId));
        if (!endGameResult.TryGetSuccessful(out var updateGame, out var endGameFailure))
        {
          return Results.BadRequest(endGameFailure.ErrorMessage);
        }

        return Results.Ok(updateGame);
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
