using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace TheGame.Api.Auth;

public static class GameAuthenticationServiceExtensions
{
  public static IServiceCollection AddGameAuthenticationServices(this IServiceCollection services, ConfigurationManager configuration)
  {
    var jwtSecret = configuration.GetValue<string>("Auth:Api:JwtSecret");
    var jwtAudience = configuration.GetValue<string>("Auth:Api:JwtAudience");

    if (string.IsNullOrEmpty(jwtSecret) || string.IsNullOrEmpty(jwtAudience))
    {
      throw new InvalidOperationException("Jwt api configuration is invalid! Both secret and audience are required!");
    }

    // see https://stackoverflow.com/questions/60858985/addopenidconnect-and-refresh-tokens-in-asp-net-core
    services
      .AddAuthentication(options =>
      {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
      })
      .AddJwtBearer(options =>
      {
        options.RequireHttpsMetadata = false; // TODO: set to true in production
        options.SaveToken = false;
        options.TokenValidationParameters = GameAuthService.GetTokenValidationParams(jwtAudience, jwtSecret, GameAuthService.ValidApiTokenIssuer);
        options.Validate();
      });

    services.AddAuthorization(authZOptions =>
    {
      authZOptions.DefaultPolicy = new AuthorizationPolicyBuilder()
        // must use jwt bearer token
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
        // must be authenticated (valid, and unexpired token)
        .RequireAuthenticatedUser()
        // must have identity user id
        .RequireClaim(GameAuthService.PlayerIdentityUserId)
        // must have player id claim
        .RequireClaim(GameAuthService.PlayerIdClaimType)
        .Build();
    });

    services.AddScoped<GameAuthService>();

    return services;
  }
}
