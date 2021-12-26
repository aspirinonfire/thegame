using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Domain
{
  public static class GameServiceExtensions
  {
    public static IServiceCollection AddGameServices(this IServiceCollection services,
      string connectionString,
      bool isDevelopment)
    {
      services
        .AddGameDataAccessServices(connectionString, isDevelopment)
        .AddScoped<IGameDbContext>(isp => isp.GetRequiredService<GameDbContext>())
        .AddScoped<ITeamService, TeamModel.TeamService>()
        .AddScoped<IGameFactory, GameModel.GameFactory>()
        .AddScoped<ILicensePlateSpotFactory, LicensePlateSpotModel.LicensePlateSpotFactory>();

      return services;
    }

    public static IServiceCollection AddGameDataAccessServices(this IServiceCollection services,
      string connectionString,
      bool isDevelopment)
    {
      if (string.IsNullOrEmpty(connectionString))
      {
        throw new ArgumentNullException(nameof(connectionString));
      }

      services.AddDbContext<GameDbContext>(options =>
      {
        if (isDevelopment)
        {
          options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
        }
        options.UseLazyLoadingProxies(true);
        // TODO configure SQL connection params
        options.UseSqlServer(connectionString);
      });

      return services;
    }
  }
}
