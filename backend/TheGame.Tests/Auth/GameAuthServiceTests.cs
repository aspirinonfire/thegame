using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TheGame.Api;
using TheGame.Api.Auth;
using TheGame.Domain.CommandHandlers;

namespace TheGame.Tests.Auth;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]

public class GameAuthServiceTests
{
  [Fact]
  public void CanGenerateJwtToken()
  {
    var gameSettings = new GameSettings()
    {
      Auth = new AuthSettings()
      { 
        Api = new ApiAuthSettings()
        {
          JwtAudience = "test_aud",
          JwtSecret = "my_very_very_long_test_secret_123!"
        },
        Google = new ApiGoogleSettings()
        {
          ClientId = "<client_id>",
          ClientSecret = "<client_secret>"
        }
      }
    };

    var gamePlayer = new GetOrCreatePlayerResult(1,
      1,
      "Test",
      "test_user_id");

    var options = Substitute.For<IOptions<GameSettings>>();
    options.Value.Returns(gameSettings);

    var uutAuthService = new GameAuthService(NullLogger<GameAuthService>.Instance,
      Substitute.For<IMediator>(),
      options);

    var actualToken = uutAuthService.GenerateApiJwtToken(gamePlayer);

    Assert.NotNull(actualToken);
  }
}
