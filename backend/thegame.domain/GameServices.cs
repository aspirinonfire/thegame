using Microsoft.Extensions.DependencyInjection;
using System;
using thegame.domain.DomainModels.Game;
using thegame.domain.DomainModels.Team;

namespace thegame.domain
{
  public static class GameServices
  {
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
      services
        .AddScoped<ITeamService, TeamModel.TeamService>()
        .AddScoped<IGameFactory, GameModel.GameFactory>();

      return services;
    }
  }
}
