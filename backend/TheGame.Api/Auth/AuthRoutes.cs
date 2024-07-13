using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public static class AuthRoutes
{
  private const string _googleSigninRoute = "signingoogleidentity";

  public static IEndpointRouteBuilder AddGameAuthRoutes(this IEndpointRouteBuilder endpoints, bool isDevEnvironment)
  {
    var accountRoute = endpoints.MapGroup("account")
      .WithDisplayName("Game User Account Routes");

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

        var signInResult = await ProcessPlayerSignIn(ctx, mediator, playerIdentityCommand);
        if (signInResult.TryGetSuccessful(out _, out var signInFailure))
        {
          return Results.LocalRedirect("/");
        }

        return Results.BadRequest(signInFailure.ErrorMessage);
      })
      .AllowAnonymous();

    if (isDevEnvironment)
    {
      accountRoute
        .MapGet("/testlogin", async (HttpContext ctx, IMediator mediator) =>
        {
          var request = new NewPlayerIdentityRequest("dev_auth",
            "dev_1234567",
            string.Empty,
            "Test Player");

          var playerIdentityCommand = new GetOrCreateNewPlayerCommand(request);

          var signInResult = await ProcessPlayerSignIn(ctx, mediator, playerIdentityCommand);
          if (signInResult.TryGetSuccessful(out _, out var signInFailure))
          {
            return Results.Ok();
          }

          return Results.BadRequest(signInFailure.ErrorMessage);
        })
        .AllowAnonymous();
    }

    return endpoints;
  }

  private static async Task<OneOf<Success, Failure>> ProcessPlayerSignIn(HttpContext context,
    IMediator mediator,
    GetOrCreateNewPlayerCommand getOrCreatePlayerCommand)
  {
    var getOrCreatePlayerResult = await mediator.Send(getOrCreatePlayerCommand);
    if (!getOrCreatePlayerResult.TryGetSuccessful(out var playerIdentity, out var playerIdentityFailure))
    {
      return playerIdentityFailure;
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
      IssuedUtc = DateTimeOffset.UtcNow
    };

    // Sign-in with Game backend and issue auth cookie
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
      claimsPrincipal,
      authProperties);

    return new Success();
  }
}
