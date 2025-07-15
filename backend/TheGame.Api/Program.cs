using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TheGame.Api.Auth;
using TheGame.Api.Common;
using TheGame.Api.Common.MessageBus;
using TheGame.Api.Endpoints;
using TheGame.Api.Endpoints.Game;
using TheGame.Api.Endpoints.User;
using TheGame.Domain;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Api;

public class Program
{
  public static async Task Main(string[] args)
  {
    var builder = WebApplication.CreateBuilder(args);
    builder.AddServiceDefaults();
    builder.WebHost.UseDefaultServiceProvider(diOpts =>
    {
      diOpts.ValidateOnBuild = true;
      diOpts.ValidateScopes = true;
    });

    string? uiHost = builder.Configuration["cors:uiHost"];
    if (!string.IsNullOrWhiteSpace(uiHost))
    {
      builder.Services.AddCors(options =>
      {
        options.AddPolicy("ui", policy =>
        {
          policy.WithOrigins(uiHost)
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
      });
    }

    // TODO switch to OpenAPI and Scalar
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

    // register game api services and configuration
    builder.Services
      .AddOptions<GameSettings>()
      .BindConfiguration("")
      .ValidateDataAnnotations()
      .ValidateOnStart();

    // Domain Services
    builder.Services.AddGameServices(sp =>
    {
      var channelsQueue = sp.GetRequiredService<ChannelsMessageQueue>();
      return new ChannelsEventBus(channelsQueue);
    });

    // API services
    builder.Services
      .AddGameAuthenticationServices(builder.Configuration)
      .AddInMemoryEventBus()
      .AddHostedService<DomainMessagesWorker>()
      .AddScoped(typeof(IDomainMessageHandler<>), typeof(DomainMessageLogger<>))
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>()
      .AddUserEndpointServices()
      .AddGameEndpointServices();

    // Set json serializer options. Both configs must be set.
    // see https://stackoverflow.com/questions/76643787/how-to-make-enum-serialization-default-to-string-in-minimal-api-endpoints-and-sw
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
      options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
      options.SerializerOptions.PropertyNameCaseInsensitive = true;
    });

    // RFC 9110 error response shape
    builder.Services.AddProblemDetails(opts =>
    {
      opts.CustomizeProblemDetails = (ctx) =>
      {
        if (string.IsNullOrEmpty(ctx.ProblemDetails.Detail))
        {
          ctx.ProblemDetails.Detail = "Please contact IT Support for assistance.";
        }

        ctx.ProblemDetails.Extensions[GameApiMiddleware.CorrelationIdKey] = ctx.HttpContext.RetrieveCorrelationId();
      };
    });

    // Swagger
    builder.Services
      .Configure<JsonOptions>(options =>
      {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
      });

    var app = builder.Build();

    if (!string.IsNullOrWhiteSpace(uiHost))
    {
      app.UseCors("ui");
    }

    // TODO need to strip out sensitive info from response
    app.UseExceptionHandler();
    app.UseStatusCodePages();

    app.MapDefaultEndpoints();

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
      app.UseHsts();
      app.UseHttpsRedirection();
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.Use(GameApiMiddleware.CreateRequestCorrelationMiddleware());

    var apiRoutes = app
      .MapGroup("api")
      .RequireAuthorization();

    apiRoutes
      .MapUserEndpoints()
      .MapGameEndpoints();

    await app.RunAsync();
  }
}
