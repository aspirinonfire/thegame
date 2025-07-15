using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading;
using TheGame.Api.Auth;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.RefreshToken;

public static class RefreshTokenEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenCommand.Result> refreshAccessTokenHandler,
    IGameAuthService authService,
    CancellationToken cancellationToken) =>
  {
    // we cannot use GetTokenAsync because expired tokens are not saved in context
    var accessToken = ctx.Request.Headers.Authorization
      .FirstOrDefault()?
      .Split(" ")
      .LastOrDefault() ?? string.Empty;

    var refreshCookieValue = authService.RetrieveRefreshTokenValue(ctx);

    var refreshResult = await refreshAccessTokenHandler.Execute(
      new RefreshAccessTokenCommand(accessToken, refreshCookieValue),
      cancellationToken);
    if (refreshResult.TryGetSuccessful(out var newAccessValues, out _))
    {
      authService.SetRefreshCookie(ctx, newAccessValues.RefreshTokenValue, newAccessValues.RefreshTokenExpiresIn);
      return Results.Ok(new { newAccessValues.AccessToken });
    }

    return refreshResult.ToHttpResponse(ctx);
  };
}
