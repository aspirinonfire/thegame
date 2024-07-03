using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using OneOf;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using TheGame.Api.Auth;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.Utils;

namespace TheGame.Api;

public sealed record StartNewGameRequest(string NewGameName);

public static class ApiRoutes
{
  public const string InvalidPlayerIdClaimError = "invalid_player_id_claim";

  public static IEndpointRouteBuilder AddGameApiRoutes(this IEndpointRouteBuilder endpoints)
  {
    var apiRoute = endpoints.MapGroup("api");

    apiRoute.MapGet("user", async (HttpContext ctx, IGameDbContext dbContext) =>
    { 
      if (!GetPlayerIdFromUserClaims(ctx.User.Claims).TryGetSuccessful(out var playerId, out var claimFailure))
      {
        return Results.BadRequest(claimFailure.ErrorMessage);
      }

      // TODO refactor to use dedicated query service
      var player = await dbContext.Players
        .AsNoTracking()
        .Where(player => player.Id == playerId)
        .Select(player => new
        {
          PlayerName = player.Name,
          PlayerId = player.Id
        })
        .FirstOrDefaultAsync();

      return Results.Ok(player);
    });

    apiRoute.MapGet("game", async (HttpContext ctx, IGameDbContext gameDbContext) =>
    {
      if (!GetPlayerIdFromUserClaims(ctx.User.Claims).TryGetSuccessful(out var playerId, out var claimFailure))
      {
        return Results.BadRequest(claimFailure.ErrorMessage);
      }

      // TODO refactor to use dedicated query service
      var ownedGames = gameDbContext
        .Games
        .AsNoTracking()
        .Where(game => game.CreatedBy.Id == playerId)
        .Select(game => new
        {
          Owner = true,
          game.Id,
          game.Name,
          game.DateCreated,
          game.DateModified,
          game.EndedOn,
          NumberOfSpottedPlates = game.GameLicensePlates.Count()
        });

      var invitedGames = gameDbContext
        .Games
        .AsNoTracking()
        .SelectMany(game => game.GamePlayerInvites)
        .Where(gameInvite => gameInvite.Player.Id == playerId)
        .Select(gameInvite => new
        {
          Owner = false,
          gameInvite.Game.Id,
          gameInvite.Game.Name,
          gameInvite.Game.DateCreated,
          gameInvite.Game.DateModified,
          gameInvite.Game.EndedOn,
          NumberOfSpottedPlates = gameInvite.Game.GameLicensePlates.Count()
        });

      var allGames = await ownedGames.Concat(invitedGames).ToListAsync();
      
      return Results.Ok(allGames);
    });

    apiRoute.MapPost("game", async (HttpContext ctx, IMediator mediator, [FromBody] StartNewGameRequest newGameRequest) =>
    {
      if (!GetPlayerIdFromUserClaims(ctx.User.Claims).TryGetSuccessful(out var playerId, out var claimFailure))
      {
        return Results.BadRequest(claimFailure.ErrorMessage);
      }

      var newGameResult = await mediator.Send(new StartNewGameCommand(newGameRequest.NewGameName, playerId));
      if (!newGameResult.TryGetSuccessful(out var newGame, out var newGameFailure))
      {
        return Results.BadRequest(newGameFailure.ErrorMessage);
      }

      return Results.Ok(newGame);

    });

    return endpoints;
  }

  public static OneOf<long, Failure> GetPlayerIdFromUserClaims(IEnumerable<Claim> claims)
  {
    var playerIdClaim = claims.FirstOrDefault(claim => claim.Type == GameAuthService.PlayerIdClaimType);

    return long.TryParse(playerIdClaim?.Value, out var playerId) ? playerId : new Failure(InvalidPlayerIdClaimError);
  }
}
