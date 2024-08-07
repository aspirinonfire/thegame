using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain;

public static class GameServiceExtensions
{
  public static IServiceCollection AddGameServices(this IServiceCollection services,
    string? connectionString = null,
    Assembly? additionalMediatrAssemblyToScan = null,
    Action<ILoggingBuilder>? efLogger = null)
  {
    services
      // Logging
      .AddLogging()
      // DB ctx
      .AddDbContext<GameDbContext>(options =>
      {
#if DEBUG
        options.UseLoggerFactory(LoggerFactory.Create(builder =>
        {
          builder.AddConsole();
          efLogger?.Invoke(builder);
        }));
        options.EnableSensitiveDataLogging(true);
#endif
        options.UseLazyLoadingProxies(true);
        options.UseSqlServer(connectionString ?? $"Name=ConnectionStrings:{GameDbContext.ConnectionStringName}",
          sqlServerOpts =>
          {
            sqlServerOpts.CommandTimeout(15);
            sqlServerOpts.EnableRetryOnFailure(3, TimeSpan.FromSeconds(2), null);
            sqlServerOpts.UseAzureSqlDefaults(true);
          });
      })
      .AddScoped<IGameDbContext>(isp => isp.GetRequiredService<GameDbContext>())
      // Utils
      .AddSingleton<ISystemService, SystemService>()
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>()
      .AddMediatR(cfg =>
      {
        cfg.RegisterServicesFromAssembly(typeof(GameServiceExtensions).Assembly);
        if (additionalMediatrAssemblyToScan != null)
        {
          cfg.RegisterServicesFromAssembly(additionalMediatrAssemblyToScan);
        }
      })
      // Game domain services
      .AddScoped<IPlayerIdentityFactory, PlayerIdentity.PlayerIdentityFactory>()
      .AddScoped<IPlayerFactory, Player.PlayerFactory>()
      .AddScoped<IPlayerQueryProvider, PlayerQueryProvider>()
      .AddScoped<IGameFactory, Game.GameFactory>()
      .AddScoped<IGameQueryProvider,  GameQueryProvider>()
      .AddScoped<IGamePlayerFactory, GamePlayer.GamePlayerFactory>()
      .AddScoped<IGameLicensePlateFactory, GameLicensePlate.LicensePlateSpotFactory>()
      .AddScoped<IGameScoreCalculator, GameScoreCalculator>();

    return services;
  }
}
