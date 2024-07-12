using MediatR;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
  public const string InvalidXsrfToken = "invalid_xsrf_token";

  public static IEndpointRouteBuilder AddGameApiRoutes(this IEndpointRouteBuilder endpoints)
  {
    var apiRoute = endpoints
      .MapGroup("api")
      // require XSRF token check for all api calls. See https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-8.0
      .WithMetadata(new AntiforgeryMetadata(true))
      // validate XSRF token
      .AddEndpointFilter(async (invocationContext, next) =>
      {
        // skip XSRF check for TRACE, OPTIONS, HEAD, and GET
        if (HttpMethods.IsTrace(invocationContext.HttpContext.Request.Method) ||
          HttpMethods.IsOptions(invocationContext.HttpContext.Request.Method) ||
          HttpMethods.IsHead(invocationContext.HttpContext.Request.Method) ||
          HttpMethods.IsGet(invocationContext.HttpContext.Request.Method))
        {
          return await next(invocationContext);
        }

        // check if XSRF token check is required for current endpoint
        var endPoint = invocationContext.HttpContext.GetEndpoint()!;
        var requireValidXsrfToken = endPoint.Metadata.Any(data => data is AntiforgeryMetadata xsrfMeta && xsrfMeta.RequiresValidation);
        if (!requireValidXsrfToken)
        {
          return await next(invocationContext);
        }

        // validate XSRF token
        try
        {
          var antiforgery = invocationContext.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();
          await antiforgery.ValidateRequestAsync(invocationContext.HttpContext);
        }
        catch (Exception)
        {
          return Results.BadRequest(InvalidXsrfToken);
        }

        // token is valid, continue pipeline execution
        return await next(invocationContext);
      })
      // extract Player ID from claims for easy access. If playerId claim is not present, short-circuit with BadRequest
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

    
    apiRoute
      .MapGet("/xsrftoken", (HttpContext ctx, IAntiforgery antiforgery) =>
      {
        return Results.Ok(new
        {
          token = antiforgery.GetAndStoreTokens(ctx).RequestToken
        });
      })
      .DisableAntiforgery();
    
    
    apiRoute
      .MapGet("user", async (HttpContext ctx, IPlayerQueryProvider playerQueryProvider) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);

        var player = await playerQueryProvider.GetPlayerInfoQuery(playerId).FirstOrDefaultAsync();

        return Results.Ok(player);
      });

    
    apiRoute
      .MapGet("game", async (HttpContext ctx, IGameQueryProvider gameQueryProvider) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);

        var allGames = await gameQueryProvider.GetOwnedAndInvitedGamesQuery(playerId).ToListAsync();
      
        return Results.Ok(allGames);
      });

    
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
      });

    
    apiRoute
      .MapPost("game/{gameId:long}/endgame", async (HttpContext ctx, IMediator mediator, [FromRoute] long gameId) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);
        var endGameResult = await mediator.Send(new EndGameCommand(gameId, playerId));
        if (!endGameResult.TryGetSuccessful(out _, out var endGameFailure))
        {
          return Results.BadRequest(endGameFailure.ErrorMessage);
        }

        return Results.Ok();
      });

    
    apiRoute
      .MapPost("game/{gameId:long}/spotplates", async (HttpContext ctx, IMediator mediator, [FromRoute] long gameId, [FromBody] IReadOnlyCollection<SpottedPlate> spottedPlates) =>
      {
        var playerId = GetPlayerIdFromHttpContext(ctx);

        var endGameResult = await mediator.Send(new SpotLicensePlatesCommand(spottedPlates, gameId, playerId));
        if (!endGameResult.TryGetSuccessful(out _, out var endGameFailure))
        {
          return Results.BadRequest(endGameFailure.ErrorMessage);
        }

        return Results.Ok();
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
