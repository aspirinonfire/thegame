using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
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

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGroup("")
      .RequireAuthorization()
      .AddGameApiRoutes();

    await app.RunAsync();
  }
}
