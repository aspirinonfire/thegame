using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Domain
{
  public static class GameServices
  {
    public static IServiceCollection AddGameServices(this IServiceCollection services)
    {
      services
        .AddScoped<ITeamService, TeamModel.TeamService>()
        .AddScoped<IGameFactory, GameModel.GameFactory>()
        .AddScoped<ILicensePlateSpotFactory, LicensePlateSpotModel.LicensePlateSpotFactory>();

      return services;
    }
  }
}
