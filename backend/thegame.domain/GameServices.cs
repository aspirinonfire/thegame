using Microsoft.Extensions.DependencyInjection;
using System;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Domain
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
