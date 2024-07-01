using Microsoft.Extensions.DependencyInjection;
using TheGame.Domain;
using TheGame.Domain.DomainModels;
using TheGame.Tests.TestUtils;

namespace TheGame.Tests
{
  [Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
  public class GameServiceExtensionTests
  {
    [Fact]
    public void CanInstantiateGameServicesDI()
    {
      var services = new ServiceCollection()
        .AddGameServices("test_conn_string", true);

      var diOpts = new ServiceProviderOptions
      {
        ValidateOnBuild = true,
        ValidateScopes = true,
      };
      using var sp = services.BuildServiceProvider(diOpts);
      using var scope = sp.CreateScope();

      var actualException = Record.Exception(scope.ServiceProvider.GetRequiredService<IGameDbContext>);

      Assert.Null(actualException);
    }
  }
}
