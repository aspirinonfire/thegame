using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using TheGame.Api.Auth;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.RefreshToken;

public static class RefreshTokenEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx, GameAuthService authService) =>
  {
    // we cannot use GetTokenAsync because expired tokens are not saved in context
    var accessToken = ctx.Request.Headers.Authorization
      .FirstOrDefault()?
      .Split(" ")
      .LastOrDefault() ?? string.Empty;

    return await authService
      .RefreshAccessToken(ctx, accessToken)
      .ToHttpResponse(ctx);
  };
}
