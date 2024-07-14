using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
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
        cfg.SwaggerDoc("v1", new OpenApiInfo() { Title = "Game API", Version = "1" });
        cfg.AddSecurityDefinition("Bearer", new()
        {
          Name = "Authorization",
          Type = SecuritySchemeType.Http,
          In = ParameterLocation.Header,
          Scheme = "bearer",
          BearerFormat = "JWT",
          Description = "Game API JWT Bearer token. See /api/user/token"
        });
        cfg.AddSecurityRequirement(new()
        {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              }
            },
            []
          }
        });
      });

    builder.Services
      .AddHealthChecks()
      .AddCheck<ApiInfraHealthCheck>(nameof(ApiInfraHealthCheck));

    var isDevEnvironment = builder.Environment.IsDevelopment();
    var connString = builder.Configuration.GetConnectionString(GameDbContext.ConnectionStringName) ?? string.Empty;

    builder.Services
      .AddGameServices(connString, isDevEnvironment)
      .AddGameAuthenticationServices(builder.Configuration);

    // set json options for API and Swagger
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
      options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
    builder.Services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
    {
      options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    builder.Services.AddHttpClient();

    builder.Services.AddOptions<GameSettings>()
      .BindConfiguration("")
      .ValidateDataAnnotations();

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

    app.UseHsts();
    app.UseHttpsRedirection();

    app.UseHealthChecks("/health");

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGroup("")
      .RequireAuthorization()
      .AddGameApiRoutes();

    // this line is required to ensure minimal api routes are executed before hitting SPA
    // see https://exploding-kitten.com/2024/08-usespa-minimal-api
    app.UseEndpoints(_ => { });

    if (app.Environment.IsDevelopment())
    {
      // redirect spa requests to local nextjs dev server. this helps with cors.
      // production will use static web apps with ui and backend deployed to separate containers/apps

      app.UseSpa(spa =>
      {
        spa.UseProxyToSpaDevelopmentServer("http://host.docker.internal:3000/");
      });
    }

    await app.RunAsync();
  }
}
