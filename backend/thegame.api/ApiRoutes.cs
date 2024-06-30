using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Linq;

namespace TheGame.Api;

public static class ApiRoutes
{
  public static IEndpointRouteBuilder AddGameApiRoutes(this IEndpointRouteBuilder endpoints)
  {
    var apiRoute = endpoints.MapGroup("api");

    apiRoute.MapGet("user", (HttpContext ctx) =>
    {
      var claimsInfo = ctx.User.Claims
        .Select(claim => new
        {
          claim.Type,
          claim.Value,
          claim.Issuer
        })
        .ToList();

      return Results.Ok(claimsInfo);
    });

    return endpoints;
  }
}
