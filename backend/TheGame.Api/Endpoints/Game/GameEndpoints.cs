using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.Game.CreateGame;
using TheGame.Api.Endpoints.Game.EndGame;
using TheGame.Api.Endpoints.Game.GetGame;
using TheGame.Api.Endpoints.Game.GetGameHistory;
using TheGame.Api.Endpoints.Game.SpotPlates;

namespace TheGame.Api.Endpoints.Game;

public static class GameEndpoints
{
  public static RouteGroupBuilder MapGameEndpoints(this RouteGroupBuilder apiRoutes)
  {
    var gameRoutes = apiRoutes.MapGroup("game");

    gameRoutes
      .MapGet("", GetGameEndpoint.Handler)
      .WithDescription("Retrieve all active games for an authenticated player.");

    gameRoutes
      .MapPost("", CreateGameEndpoint.Handler)
      .WithDescription("Start new game for an authenticated player.");

    gameRoutes
      .MapPost("{gameId:long}/endgame", EndGameEndpoint.Handler)
      .WithDescription("End active game for an authenticated player.");

    gameRoutes
      .MapPost("{gameId:long}/spotplates", SpotPlatesEndpoint.Handler)
      .WithDescription("Updated spotted license plates for an active game.");

    gameRoutes
      .MapGet("history", GetGameHistoryEndpoint.Handler)
      .WithDescription("Retrieve game history stats");

    return apiRoutes;
  }

  public static IServiceCollection AddGameEndpointServices(this IServiceCollection services)
  {
    services
    .AddScoped<
      ICommandHandler<StartNewGameCommand, OwnedOrInvitedGame>,
      StartNewGameCommandHandler>()
    .AddScoped<
      ICommandHandler<EndGameCommand, OwnedOrInvitedGame>,
      EndGameCommandHandler>()
    .AddScoped<
      ICommandHandler<SpotLicensePlatesCommand, OwnedOrInvitedGame>,
      SpotLicensePlatesCommandHandler>()
    .AddScoped<IGameQueryProvider, GameQueryProvider>();

    return services;
  }
}
