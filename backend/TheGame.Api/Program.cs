using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System;
using System.Linq;
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
    // Containerized API project "knows" how to execute EF migrations correctly without additional tooling.
    // Migrations shouldn't be executed every time app starts but they can be executed in Init container context.
    // To avoid creating additional image and de-couple required migrations from the business code,
    // this project image can be executed in db migration mode only.
    // To achieve this, init container must have the following:
    // 1. start with --migrate-db arg: dotnet TheGame.Api --migrate-db
    // 2. include connection string in env vars with the same key as app (eg ConnectionStrings__GameDb)
    if (args.Any(arg => "--migrate-db".Equals(arg, StringComparison.OrdinalIgnoreCase)))
    {
      await RunDbMigrations();
      return;
    }

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

    // register game api services
    builder.Services
      .AddOptions<GameSettings>()
      .BindConfiguration("")
      .ValidateDataAnnotations();

    builder.Services
      .AddGameServices(connString, typeof(Program).Assembly)
      .AddGameAuthenticationServices(builder.Configuration);

    // Set json serializer options. Both configs must be set.
    // see https://stackoverflow.com/questions/76643787/how-to-make-enum-serialization-default-to-string-in-minimal-api-endpoints-and-sw

    // Minimal APIs
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
      options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
      options.SerializerOptions.PropertyNameCaseInsensitive = true;
    });
    // Swagger
    builder.Services
      .Configure<JsonOptions>(options =>
      {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
      });

    builder.AddGameApiOpenTelemtry();

    var app = builder.Build();

    app.UseDefaultFiles();  // re-write path only. / to /index.html
    app.UseStaticFiles();   // serve ui files from wwwroot

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

    app.UseHealthChecks("/api/health");

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGroup("")
      .RequireAuthorization()
      .AddGameApiRoutes();

    // fallback to spa
    app.MapFallbackToFile("/index.html");

    await app.RunAsync();
  }

  private static async Task RunDbMigrations()
  {
    try
    {
      Console.WriteLine("Executing EF Migrations");
      
      var connectionString = Environment.GetEnvironmentVariable($"ConnectionStrings__{GameDbContext.ConnectionStringName}");

      var services = new ServiceCollection()
        .AddGameServices(connectionString ?? string.Empty);

      await using var sp = services.BuildServiceProvider();

      var dbContext = (GameDbContext)sp.GetRequiredService<IGameDbContext>();

      using var trx = await dbContext.Database.BeginTransactionAsync();

      await dbContext.Database.MigrateAsync();

      await trx.CommitAsync();
      
      Console.WriteLine("EF Migrations executed successfully!");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"Failed to execute migrations due to unhandled exception. {ex.GetType().Name}: {ex.Message}");
      // TODO re-enable throw once everything is working.
      //throw;
    }
  }
}
