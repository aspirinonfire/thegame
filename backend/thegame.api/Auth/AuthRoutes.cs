using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using TheGame.Domain.Utils;

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
      .MapGet($"/{_googleSigninRoute}", async (HttpContext ctx, GameAuthService gameAuthService, IMediator mediator) =>
      {
        var googleAuthResult = await ctx.AuthenticateAsync(OpenIdConnectDefaults.AuthenticationScheme);
        if (googleAuthResult == null)
        {
          return Results.BadRequest("Unable to authenticate google auth result");
        }

        var commandGenerationResult = gameAuthService.GenerateGetOrCreateNewPlayerCommand(googleAuthResult);
        if (!commandGenerationResult.TryGetSuccessful(out var playerIdentityCommand, out var generateCommandFailures))
        {
          return Results.BadRequest(generateCommandFailures.ErrorMessage);
        }

        var getOrCreatePlayerResult = await mediator.Send(playerIdentityCommand);
        if (!getOrCreatePlayerResult.TryGetSuccessful(out var playerIdentity, out var playerIdentityFailure))
        {
          return Results.BadRequest(playerIdentityFailure.ErrorMessage);
        }

        // Create claims principal
        var claims = new List<Claim>
        {
          new(GameAuthService.PlayerIdClaimType, $"{playerIdentity.PlayerId}", ClaimValueTypes.String),
          new(GameAuthService.PlayerIdentityIdClaimType, $"{playerIdentity.PlayerIdentityId}", ClaimValueTypes.String),
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

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
        return Results.LocalRedirect("/api/user");
      })
      .AllowAnonymous();

    return endpoints;
  }
}
