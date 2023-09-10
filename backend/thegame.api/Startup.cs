using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using TheGame.Api.Auth;
using TheGame.Domain;
using TheGame.Domain.DAL;

namespace TheGame.Api
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      var connString =  Configuration.GetConnectionString(GameDbContext.ConnectionStringName);
      if (string.IsNullOrEmpty(connString))
      {
        throw new ApplicationException($"{GameDbContext.ConnectionStringName} connection string is not found! Aborting...");
      }
      // TODO env check
      var isDevelopment = true;
      services.AddGameServices(connString, isDevelopment);

      services.AddControllers();
      services.AddSwaggerGen(c =>
      {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "thegame.api", Version = "v1" });
      });

      AddAppIdentityServices(services,
        Configuration["Auth:Google:ClientId"],
        Configuration["Auth:Google:ClientSecret"]);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app,
      IWebHostEnvironment env,
      ILogger<Startup> logger,
      GameDbContext dbContext)
    {
      //if (!dbContext.Database.CanConnect())
      //{
      //  const string msg = "Could not connect to database! Aborting...";
      //  logger.LogCritical(msg);
      //  throw new ApplicationException(msg);
      //}

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "thegame.api v1"));
      }

      app.UseHttpsRedirection();

      var cookiePolicyOptions = new CookiePolicyOptions
      {
        MinimumSameSitePolicy = SameSiteMode.Lax,
        HttpOnly = HttpOnlyPolicy.Always,
        Secure = CookieSecurePolicy.Always
      };
      app.UseCookiePolicy(cookiePolicyOptions);

      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        // all controllers require authorization by default
        endpoints
          .MapControllers()
          .RequireAuthorization();
      });
    }

    private static IServiceCollection AddAppIdentityServices(IServiceCollection services,
      string? googleClientId,
      string? googleClientSecret)
    {
      // TODO validate google creds

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

          googleAuthOpts.ClientId = googleClientId ?? string.Empty;
          googleAuthOpts.ClientSecret = googleClientSecret ?? string.Empty;

          googleAuthOpts.SaveTokens = true;
          googleAuthOpts.AccessType = "offline";            // retrieve refresh tokens
          googleAuthOpts.CallbackPath = "/signin-google";   // special callback URL. This route will be handled by Google middleware.
        })
        // OpenId cookie config
        .AddCookie(OpenIdConnectDefaults.AuthenticationScheme)
        // Auth cookie config
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

            cookieOpts.Validate();
          });

      services.AddAuthorization(authZOptions =>
      {
        authZOptions.DefaultPolicy = new AuthorizationPolicyBuilder()
          // cookie auth only
          .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
          // must be authenticated
          .RequireAuthenticatedUser()
          // required to have player id claim
          .RequireClaim(GameAuthService.PlayerIdClaimType)
          .Build();
      });

      // TODO add antiforgery! Requires app ui changes

      services.AddSingleton<GameAuthService>();

      return services;
    }
  }
}
