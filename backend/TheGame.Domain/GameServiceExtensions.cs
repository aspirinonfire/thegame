using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using TheGame.Domain.Commands;
using TheGame.Domain.Commands.CreateTeamAndPlayer;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

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
        }
        options.UseLazyLoadingProxies(true);
        options.UseSqlServer(connectionString);
      })
      .AddScoped<IGameDbContext>(isp => isp.GetRequiredService<GameDbContext>())
      // Utils
      .AddSingleton<ISystemService, SystemService>()
      .AddMediatR(cfg =>
      {
        cfg.RegisterServicesFromAssembly(typeof(GameServiceExtensions).Assembly);
      })
      // Game domain services
      .AddScoped<IPlayerFactory, Player.PlayerFactory>()
      .AddScoped<IGameFactory, Game.GameFactory>()
      .AddScoped<IGamePlayerFactory, GamePlayer.GamePlayerFactory>()
      .AddScoped<IGameLicensePlateFactory, GameLicensePlate.LicensePlateSpotFactory>()
      // Commands
      .AddScoped<IRequestHandler<CreateTeamAndPlayerCommand, CommandResult<CreateTeamAndPlayerResult>>, CreateTeamAndPlayerCommandHandler>();

    return services;
  }
}
