using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace TheGame.Api.Auth;

public static class GameAuthenticationServiceExtensions
{
  public static void AddGameAuthenticationServices(this IServiceCollection services, ConfigurationManager configuration)
  {
    // see https://stackoverflow.com/questions/60858985/addopenidconnect-and-refresh-tokens-in-asp-net-core
    services
      .AddAuthentication(options =>
      {
        // using cookie auth because app is web based
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
      })
      // Google Auth
      .AddGoogle(googleAuthOpts =>
      {
        // Use OpenId auth scheme cookie
        googleAuthOpts.SignInScheme = OpenIdConnectDefaults.AuthenticationScheme;

        // TODO use strongly typed config object
        // TODO validate google creds (Data annotations and IOptions)
        // TODO Fix magic strings
        googleAuthOpts.ClientId = configuration.GetValue<string>("Auth:Google:ClientId") ?? string.Empty;
        googleAuthOpts.ClientSecret = configuration.GetValue<string>("Auth:Google:ClientSecret") ?? string.Empty;

        googleAuthOpts.SaveTokens = true;
        googleAuthOpts.AccessType = "offline";            // retrieve refresh tokens
        googleAuthOpts.CallbackPath = "/signin-google";   // special callback URL. This route will be handled by Google middleware.
      })
      // OpenId cookie config
      .AddCookie(OpenIdConnectDefaults.AuthenticationScheme)
      // Auth cookie config
      // TODO use JWT instead of cookies
      .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme,
        cookieOpts =>
        {
          cookieOpts.SlidingExpiration = true;
          cookieOpts.LoginPath = "/account/google-login";

          cookieOpts.ExpireTimeSpan = TimeSpan.FromMinutes(60);     // auth ticket lifespan
          cookieOpts.Cookie.Name = "GameApi.Auth.Biscuit";
          cookieOpts.Cookie.MaxAge = TimeSpan.FromMinutes(60);      // cookie lifespan
          cookieOpts.Cookie.IsEssential = true;
          cookieOpts.Cookie.HttpOnly = true;
          cookieOpts.Cookie.SecurePolicy = CookieSecurePolicy.Always;
          // TODO revisit. Strict brakes google oauth
          cookieOpts.Cookie.SameSite = SameSiteMode.Lax;

          cookieOpts.ClaimsIssuer = "GameApi";

          // validate cookie principal
          cookieOpts.Events.OnValidatePrincipal = async (ctx) =>
          {
            var validator = ctx.HttpContext.RequestServices.GetRequiredService<GameAuthService>();
            await validator.RefreshCookie(ctx);
          };

          cookieOpts.Events.OnRedirectToLogin = ctx =>
          {
            // return 401 on non-account requests or redirect to login
            if (!ctx.Request.Path.Value?.StartsWith("/account", StringComparison.OrdinalIgnoreCase) ?? true)
            {
              ctx.Response.Clear();
              ctx.Response.StatusCode = 401;
            }
            else
            {
              ctx.Response.Redirect(ctx.RedirectUri);
            }

            return Task.CompletedTask;
          };

          cookieOpts.Validate();
        });

    services.AddAuthorization(authZOptions =>
    {
      authZOptions.DefaultPolicy = new AuthorizationPolicyBuilder()
        // cookie auth only
        // TODO use JWT
        .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
        // must be authenticated
        .RequireAuthenticatedUser()
        // required to have player id claim
        .RequireClaim(GameAuthService.PlayerIdClaimType)
        .Build();
    });

    // TODO add antiforgery!!!

    services.AddSingleton<GameAuthService>();
  }
}
