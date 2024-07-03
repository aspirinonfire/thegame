using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Domain;

public static class GameServiceExtensions
{
  public static IServiceCollection AddGameServices(this IServiceCollection services,
    string connectionString,
    bool isDevelopment)
  {
    if (string.IsNullOrEmpty(connectionString))
    {
      throw new ArgumentNullException(nameof(connectionString));
    }

    services
      // Logging
      .AddLogging()
      // DB ctx
      .AddDbContext<GameDbContext>(options =>
      {
        if (isDevelopment)
        {
          options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
          options.EnableSensitiveDataLogging(true);
        }
        options.UseLazyLoadingProxies(true);
        options.UseSqlServer(connectionString);
      })
      .AddScoped<IGameDbContext>(isp => isp.GetRequiredService<GameDbContext>())
      // Utils
      .AddSingleton<ISystemService, SystemService>()
      .AddScoped<ITransactionExecutionWrapper, TransactionExecutionWrapper>()
      .AddMediatR(cfg =>
      {
        cfg.RegisterServicesFromAssembly(typeof(GameServiceExtensions).Assembly);
      })
      // Game domain services
      .AddScoped<IPlayerIdentityFactory, PlayerIdentity.PlayerIdentityFactory>()
      .AddScoped<IPlayerFactory, Player.PlayerFactory>()
      .AddScoped<IPlayerQueryProvider, PlayerQueryProvider>()
      .AddScoped<IGameFactory, Game.GameFactory>()
      .AddScoped<IGameQueryProvider,  GameQueryProvider>()
      .AddScoped<IGamePlayerFactory, GamePlayer.GamePlayerFactory>()
      .AddScoped<IGameLicensePlateFactory, GameLicensePlate.LicensePlateSpotFactory>();

    return services;
  }
}
