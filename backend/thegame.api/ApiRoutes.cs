using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using TheGame.Api.Auth;
using TheGame.Domain.DomainModels;

namespace TheGame.Api;

public static class ApiRoutes
{
  public static IEndpointRouteBuilder AddGameApiRoutes(this IEndpointRouteBuilder endpoints)
  {
    var apiRoute = endpoints.MapGroup("api");

    apiRoute.MapGet("user", async (HttpContext ctx, IGameDbContext dbContext) =>
    {
      // TODO create helper service to generate 

      var playerId = ctx.User.Claims
        .Where(claim => claim.Type == GameAuthService.PlayerIdClaimType)
        .Select(claim => Convert.ToInt64(claim.Value))
        .FirstOrDefault();

      if (playerId < 1)
      {
        return Results.BadRequest("Invalid Player Id claim");
      }

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

    return endpoints;
  }
}
