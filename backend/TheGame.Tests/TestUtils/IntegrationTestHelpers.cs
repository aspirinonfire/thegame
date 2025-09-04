using Microsoft.Extensions.DependencyInjection;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.User;

namespace TheGame.Tests.TestUtils;

public static class IntegrationTestHelpers
{
  public static async Task<TResult> RunAsScopedRequest<TCommand, TResult>(IServiceProvider serviceProvider, TCommand command)
    where TCommand : class
    where TResult : class
  {
    await using var scope = serviceProvider.CreateAsyncScope();
    var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();

    var result = await handler.Execute(command, CancellationToken.None);
    result.AssertIsSucceessful(out var successfulResult);
    return successfulResult;
  }

  public static async Task<GetOrCreatePlayerRequest.Result> CreatePlayerWithIdentity(IServiceProvider serviceProvider, GetOrCreatePlayerRequest request)
  {
    await using var scope = serviceProvider.CreateAsyncScope();
    var playerService = scope.ServiceProvider.GetRequiredService<IPlayerService>();
    var getOrAddPlayerResult = await playerService.GetOrCreatePlayer(request, CancellationToken.None);
    return getOrAddPlayerResult.AssertIsSucceessful();
  }
}
