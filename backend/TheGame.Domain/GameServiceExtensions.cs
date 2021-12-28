using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using TheGame.Domain.DAL;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Teams;
using TheGame.Domain.Utils;

namespace TheGame.Domain
{
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
          // TODO configure SQL connection params
          options.UseSqlServer(connectionString);
        })
        .AddScoped<IGameDbContext>(isp => isp.GetRequiredService<GameDbContext>())
        // Utils
        .AddSingleton<ISystemService, SystemService>()
        .AddMediatR(typeof(GameServiceExtensions).GetTypeInfo().Assembly)
        // Game services
        .AddScoped<ITeamService, Team.TeamService>()
        .AddScoped<IGameFactory, Game.GameFactory>()
        .AddScoped<ILicensePlateSpotFactory, LicensePlateSpot.LicensePlateSpotFactory>();

      return services;
    }
  }
}
