using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.User.GetUser;
using TheGame.Api.Endpoints.User.GoogleApiToken;
using TheGame.Api.Endpoints.User.RefreshToken;

namespace TheGame.Api.Endpoints.User;

public static class UserEndpoints
{
  public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder apiRoutes)
  {
    var userRoutes = apiRoutes.MapGroup("user");

    userRoutes
      .MapGet("userData", GetUserDataEndpoint.Handler)
      .WithDescription("Get user data for authenticated player.");

    userRoutes
      .MapPost("google/apitoken", GoogleApiTokenEndpoint.Handler)
      .AllowAnonymous()
      .WithDescription("Validate Google Auth Code and generate API tokens for accessing Game APIs.");

    userRoutes
      .MapPost("refresh-token", RefreshTokenEndpoint.Handler)
      .AllowAnonymous()
      .WithDescription("Refresh Game API Token using Refresh Cookie.");

    return apiRoutes;
  }

  public static IServiceCollection AddUserEndpointServices(this IServiceCollection services)
  {
    services
      .AddScoped<
        ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenCommand.Result>,
        RefreshAccessTokenCommandHandler>()
      .AddScoped<
        ICommandHandler<AuthenticateWithGoogleAuthCodeCommand, AuthenticateWithGoogleAuthCodeCommand.Result>,
        AuthenticateWithGoogleAuthCodeCommandHandler>()
      .AddScoped<IPlayerService, PlayerService>();

    return services;
  }
}
