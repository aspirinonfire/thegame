using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using TheGame.Api.Auth;
using TheGame.Api.Common;

namespace TheGame.Api.Endpoints.User.GoogleApiToken;

public static class GoogleApiTokenEndpoint
{
  public readonly static Delegate Handler = async (HttpContext ctx, GameAuthService authService, [FromBody] string credential) =>
  {
    return await authService
      .AuthenticateWithGoogleAuthCode(credential, ctx)
      .ToHttpResponse(ctx);
  };
}
