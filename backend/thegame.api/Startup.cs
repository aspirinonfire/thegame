using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using TheGame.Api.Security;
using TheGame.Api.Security.Models;
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
      if (!dbContext.Database.CanConnect())
      {
        const string msg = "Could not connect to database! Aborting...";
        logger.LogCritical(msg);
        throw new ApplicationException(msg);
      }

      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "thegame.api v1"));
      }

      app.UseHttpsRedirection();
      app.UseRouting();

      app.UseAuthentication();
      app.UseAuthorization();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllers();
      });
    }

    private static IServiceCollection AddAppIdentityServices(IServiceCollection services,
      string googleClientId,
      string googleClientSecret)
    {
      services.AddDbContext<AppUserIdentityDbContext>(config =>
      {
        // for in memory database  
        config.UseInMemoryDatabase("MemoryBaseDataBase");
      });

      services
        .AddIdentity<AppUser, IdentityRole>()
        .AddEntityFrameworkStores<AppUserIdentityDbContext>()
        .AddDefaultTokenProviders();

      services
        .AddAuthentication(options =>
        {
          // using cookie auth because app is web based
          options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
          options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
        })
        .AddCookie()
        .AddOpenIdConnect(authenticationScheme: "Google", "Google", googleAuthOpts =>
        {
          // https://stackoverflow.com/a/52493428
          googleAuthOpts.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

          googleAuthOpts.Authority = "https://accounts.google.com";
          googleAuthOpts.ClientId = googleClientId;
          googleAuthOpts.ClientSecret = googleClientSecret;
          googleAuthOpts.SaveTokens = true;

          googleAuthOpts.CallbackPath = "/signin-google";
        });

      return services;
    }
  }
}
