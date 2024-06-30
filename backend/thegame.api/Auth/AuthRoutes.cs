using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;

namespace TheGame.Api.Auth;

public static class AuthRoutes
{
  private const string _googleSigninRoute = "signingoogle";

  public static IEndpointRouteBuilder AddGameAuthRoutes(this IEndpointRouteBuilder endpoints)
  {
    var accountRoute = endpoints.MapGroup("account");

    accountRoute
      .MapGet("/login", (HttpContext ctx) =>
      {
        var properties = new AuthenticationProperties
        {
          RedirectUri = $"https://{ctx.Request.Host}/account/{_googleSigninRoute}",
          AllowRefresh = true,
        };

        var result = Results.Challenge(properties, ["Google"]);
        return result;
      })
      .AllowAnonymous();

    accountRoute
      .MapGet($"/{_googleSigninRoute}", async (HttpContext ctx, GameAuthService gameAuthService) =>
      {
        var googleAuthResult = await ctx.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
        if (googleAuthResult == null)
        {
          return Results.BadRequest("Unable to authenticate google auth result");
        }

        // Parse auth results
        var claimsPrincipalResult = await gameAuthService.CreateClaimsIdentity(googleAuthResult);
        if (!claimsPrincipalResult.TryPickT0(out var claimsPrincipal, out var principalError))
        {
          return Results.BadRequest(principalError);
        }

        // create auth cookie
        var authProperties = new AuthenticationProperties
        {
          AllowRefresh = true,
          ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
          IsPersistent = true,
          IssuedUtc = DateTimeOffset.UtcNow,
        };

        // Remove OpenId challenge cookie
        await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);

        // Sign-in with Game backend and issue auth cookie
        await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
          claimsPrincipal,
          authProperties);

        // TODO redirect to UI landing page
        return Results.Ok(new { claimsPrincipal.Identity?.Name });
      })
      .AllowAnonymous();

    return endpoints;
  }
}
