using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using TheGame.Api.Auth;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.RefreshToken;

public sealed record RefreshTokenDto(string AccessToken, string IdToken, string IdentityProvider);

public static class RefreshTokenEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx,
    [FromBody]RefreshTokenDto refreshTokenDto,
    ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenCommand.Result> refreshAccessTokenHandler,
    IGameAuthService authService,
    CancellationToken cancellationToken) =>
  {
    var refreshCookieValue = authService.RetrieveRefreshTokenValue(ctx);

    var refreshResult = await refreshAccessTokenHandler.Execute(
      new RefreshAccessTokenCommand(refreshTokenDto.AccessToken, refreshCookieValue),
      cancellationToken);
    
    if (refreshResult.TryGetSuccessful(out var newAccessValues, out var failure))
    {
      authService.SetRefreshCookie(ctx, newAccessValues.RefreshTokenValue, newAccessValues.RefreshTokenExpiresIn);
      return Results.Ok(new { newAccessValues.AccessToken });
    }

    return failure.ToHttpResponse(ctx);
  };
}
