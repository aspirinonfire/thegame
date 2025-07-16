using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using TheGame.Api.Auth;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.GoogleApiToken;

public static class GoogleApiTokenEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    ICommandHandler<AuthenticateWithGoogleAuthCodeCommand, AuthenticateWithGoogleAuthCodeCommand.Result> googleAuthCodeCommandHandler,
    IGameAuthService gameAuthService,
    CancellationToken cancellationToken,
    [FromBody] string credential) =>
  {
    var authResult = await googleAuthCodeCommandHandler.Execute(new AuthenticateWithGoogleAuthCodeCommand(credential),
      cancellationToken);
    
    if (authResult.TryGetSuccessful(out var apiTokens, out var failure))
    {
      gameAuthService.SetRefreshCookie(ctx, apiTokens.RefreshTokenValue, apiTokens.RefreshTokenExpiresIn);
      return Results.Ok(new { apiTokens.AccessToken });
    }

    return failure.ToHttpResponse(ctx);
  };
}
