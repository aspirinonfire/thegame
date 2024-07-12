using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TheGame.Api.Auth;
using TheGame.Domain;
using TheGame.Domain.DomainModels;

namespace TheGame.Api;

public class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);
    builder.WebHost.UseDefaultServiceProvider(diOpts =>
    {
      diOpts.ValidateOnBuild = true;
      diOpts.ValidateScopes = true;
    });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services
      .AddEndpointsApiExplorer()
      .AddSwaggerGen(cfg =>
      {
        cfg.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Title = "Game API", Version = "1" });
      });

    builder.Services.AddHealthChecks()
      .AddCheck<ApiInfraHealthCheck>(nameof(ApiInfraHealthCheck));

    var isDevEnvironment = builder.Environment.IsDevelopment();
    var connString = builder.Configuration.GetConnectionString(GameDbContext.ConnectionStringName) ?? string.Empty;

    builder.Services
      .AddGameServices(connString, isDevEnvironment)
      .AddGameAuthenticationServices(builder.Configuration);

    builder.Services.AddSpaStaticFiles(options =>
    {
      // TODO configure root path correctly!
      options.RootPath = "../../ui/next_out";
    });

    builder.Services.ConfigureHttpJsonOptions(options =>
    {
      options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    {
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddAntiforgery(csrfOpts =>
    {
      csrfOpts.HeaderName = "X-XSRF-TOKEN";
      csrfOpts.SuppressXFrameOptionsHeader = false;
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (isDevEnvironment)
    {
      app.UseDeveloperExceptionPage();
      app.UseSwagger();
      app.UseSwaggerUI(swaggerOpts =>
      {
        swaggerOpts.SwaggerEndpoint("/swagger/v1/swagger.json", "Api v1");
      });
    }
    else
    {
      app.UseSpaStaticFiles();
    }

    app.UseHsts();
    app.UseHttpsRedirection();

    app.UseHealthChecks("/health");

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

    // the middleware must be placed AFTER UseRouting, UseAuth..., and be before UseEndpoints
    app.UseAntiforgery();

    app.MapGroup("")
      .RequireAuthorization()
      .AddGameAuthRoutes(isDevEnvironment)
      .AddGameApiRoutes();

    // this line is required to ensure minimal api routes are executed before hitting SPA
    // see https://exploding-kitten.com/2024/08-usespa-minimal-api
    app.UseEndpoints(_ => { });

    app.UseSpa(spa =>
    { 
      if (app.Environment.IsDevelopment())
      {
        // redirect spa requests to local nextjs dev server
        spa.UseProxyToSpaDevelopmentServer("http://host.docker.internal:3000/");
      }
    });

    await app.RunAsync();
  }
}
