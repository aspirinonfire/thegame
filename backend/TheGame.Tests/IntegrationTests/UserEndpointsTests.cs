using Azure.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MockQueryable;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using TheGame.Api;
using TheGame.Api.Auth;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.User;
using TheGame.Api.Endpoints.User.GetUser;
using TheGame.Api.Endpoints.User.GoogleApiToken;
using TheGame.Api.Endpoints.User.RefreshToken;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Common;

namespace TheGame.Tests.Endpoints.User;

[Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
public class UserEndpointsTests
{
  private const string _testJwtSecret = "this is a jwt secret value for testing api routes!";
  private const string _testJwtAudience = "test audience";
  private const ushort _testJwtExpirationMin = 1;
  private const string _refreshTokenId = "refresh-token-id";

  [Fact]
  public async Task CanRunHealthCheckRouteWithoutAuthentication()
  {
    await using var uutApiApp = GetApiFactory();
    var client = uutApiApp.CreateClient();

    var actualApiResponse = await client.GetAsync("/health");

    Assert.Equal(HttpStatusCode.OK, actualApiResponse.StatusCode);

    var actualResponseBody = await actualApiResponse.Content.ReadAsStringAsync();
    Assert.Equal("Healthy", actualResponseBody);
  }

  [Fact]
  public async Task WillReturn401ForUnauthenticatedUserWhenAccessingApiRoutes()
  {
    await using var uutApiApp = GetApiFactory();
    var client = uutApiApp.CreateClient();

    var actualApiResponse = await client.GetAsync("/api/user/userData");

    Assert.Equal(HttpStatusCode.Unauthorized, actualApiResponse.StatusCode);
  }

  [Fact]
  public async Task WillReturn401ForExpiredAccessTokenWhenAccessingApiRoutes()
  {
    var currentAccessToken = GetExpiredApiAccessToken(1, _refreshTokenId);

    await using var uutApiApp = GetApiFactory();
    var client = uutApiApp.CreateClient();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", currentAccessToken);

    var actualApiResponse = await client.GetAsync("/api/user/userData");

    Assert.Equal(HttpStatusCode.Unauthorized, actualApiResponse.StatusCode);
  }

  [Fact]
  public async Task CanGetPlayerDataForAuthenticatedUser()
  {
    var testPlayerId = 123;

    var expectedUserData = new UserData(
      new PlayerInfo("Test Player", testPlayerId),
      []);

    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddScoped(sp =>
      {
        var mockQueryProvider = Substitute.For<IPlayerService>();
        mockQueryProvider
          .GetPlayerInfoQuery(testPlayerId)
          .Returns(new[] { expectedUserData.Player }.BuildMock());

        return mockQueryProvider;
      });

      services.AddScoped(sp =>
      {
        var gameQueryProvider = Substitute.For<IGameQueryProvider>();
        gameQueryProvider
          .GetOwnedAndInvitedGamesQuery(testPlayerId)
          .Returns([]);

        return gameQueryProvider;
      });
    });

    var client = CreateAuthenticatedClient(uutApiApp, testPlayerId, _refreshTokenId);

    var actualApiResponse = await client.GetAsync("/api/user/userData");

    Assert.Equal(HttpStatusCode.OK, actualApiResponse.StatusCode);

    var actualPlayerData = await actualApiResponse.Content.ReadFromJsonAsync<UserData>();
    Assert.NotNull(actualPlayerData);
    Assert.Equal(expectedUserData.Player, actualPlayerData.Player);
    Assert.Empty(actualPlayerData.ActiveGames);
  }

  [Fact]
  public async Task WillAuthenticateNewPlayerWithWhenGoogleAuthCodeIsValid()
  {
    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddScoped(sp =>
      {
        var cmdHandler = Substitute
          .For<ICommandHandler<AuthenticateWithAuthCodeCommand, AuthenticateWithAuthCodeCommand.Result>>();

        cmdHandler
          .Execute(Arg.Is<AuthenticateWithAuthCodeCommand>(cmd => cmd.AuthCode == "test-auth-code"), Arg.Any<CancellationToken>())
          .Returns(new AuthenticateWithAuthCodeCommand.Result(true,
            "test-access-token",
            "test-refresh-token",
            TimeSpan.FromSeconds(60)));

        return cmdHandler;
      });

      services.AddScoped<IGameAuthService>(sp => Substitute.ForPartsOf<GameAuthService>(Substitute.For<ICryptoHelper>(),
        CommonMockedServices.GetMockedTimeProvider(),
        NullLogger<GameAuthService>.Instance,
        Substitute.For<IOptions<GameSettings>>()
        ));
    });

    var client = uutApiApp.CreateClient();

    var actualAuthTokenResponseMessage = await client.PostAsJsonAsync("/api/user/google/apitoken", "test-auth-code");

    Assert.Equal(HttpStatusCode.OK, actualAuthTokenResponseMessage.StatusCode);

    var actualCookiesFromResponse = actualAuthTokenResponseMessage.Headers
      .Where(header => header.Key == "Set-Cookie")
      .SelectMany(header => header.Value)
      .Select(cookie =>
      {
        var cookieParts = cookie.Split("=");

        return new
        {
          Name = cookieParts[0],
          Value = string.Join("=", cookieParts[1..])
        };
      })
      .ToDictionary(x => x.Name, x => x.Value);

    var actualRefreshCookieValue = Assert.Contains(GameAuthService.ApiRefreshTokenCookieName, actualCookiesFromResponse);
    Assert.NotNull(actualRefreshCookieValue);
    Assert.Contains("secure", actualRefreshCookieValue);
    Assert.Contains("samesite=none", actualRefreshCookieValue);
    Assert.Contains("httponly", actualRefreshCookieValue);
    Assert.Contains("path=/", actualRefreshCookieValue);

    var actualResponse = await actualAuthTokenResponseMessage.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    Assert.NotNull(actualResponse);
    var actualToken = Assert.Contains("accessToken", actualResponse);
    Assert.Equal("test-access-token", actualToken.ToString());
  }

  [Fact]
  public async Task WillRefreshTokenWithValidAccessTokenAndRefreshCookie()
  {
    var currentRefreshToken = "current_refresh";
    var playerId = 123L;
    
    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddTransient(sp =>
      {
        var refreshTokenCommandHandler =
          Substitute.For<ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenCommand.Result>>();
        refreshTokenCommandHandler
          .Execute(Arg.Any<RefreshAccessTokenCommand>(), Arg.Any<CancellationToken>())
          .Returns(new RefreshAccessTokenCommand.Result(
            "new_access_token",
            "new_refresh_token",
            TimeSpan.FromSeconds(10)
          ));

        return refreshTokenCommandHandler;
      });
    });

    await using var scope = uutApiApp.Services.CreateAsyncScope();

    var gameAuthService = scope.ServiceProvider.GetRequiredService<IGameAuthService>();

    var currentAccessToken = gameAuthService.GenerateApiJwtToken("test provider",
      "test provider user id",
      playerId,
      playerId,
      _refreshTokenId);

    var client = uutApiApp.CreateClient();
    client.DefaultRequestHeaders.Add("Cookie", $"gameapi-refresh={currentRefreshToken}");

    using var content = JsonContent.Create(new
    {
      accessToken = currentAccessToken
    });

    var actualAuthTokenResponseMessage = await client.PostAsync("/api/user/refresh-token", content);

    Assert.Equal(HttpStatusCode.OK, actualAuthTokenResponseMessage.StatusCode);

    var actualResponse = await actualAuthTokenResponseMessage.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    Assert.NotNull(actualResponse);
    var actualNewAccessToken = Assert.Contains("accessToken", actualResponse);
    Assert.NotEqual(currentAccessToken, actualNewAccessToken);

    var actualCookiesFromResponse = actualAuthTokenResponseMessage.Headers
      .Where(header => header.Key == "Set-Cookie")
      .SelectMany(header => header.Value)
      .Select(cookie =>
      {
        var cookieParts = cookie.Split("=");

        return new
        {
          Name = cookieParts[0],
          Value = string.Join("=", cookieParts[1..])
        };
      })
      .ToDictionary(x => x.Name, x => x.Value);

    var actualNewRefreshCookieValue = Assert.Contains(GameAuthService.ApiRefreshTokenCookieName, actualCookiesFromResponse);
    Assert.NotNull(actualNewRefreshCookieValue);
    Assert.Contains("new_refresh_token;", actualNewRefreshCookieValue);
  }

  [Fact]
  public async Task WillRefreshTokenWithExpiredAccessTokenAndValidRefreshCookie()
  {
    var currentRefreshToken = "current_refresh";
    var playerId = 123L;

    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddTransient(sp =>
      {
        var handler = Substitute.For<ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenCommand.Result>>();
        handler
          .Execute(Arg.Any<RefreshAccessTokenCommand>(), Arg.Any<CancellationToken>())
          .Returns(new RefreshAccessTokenCommand.Result(
            "new_access_token",
            "new_refresh_token",
            TimeSpan.FromSeconds(10)
          ));
        return handler;
      });
    });
    await using var scope = uutApiApp.Services.CreateAsyncScope();

    var gameAuthService = scope.ServiceProvider.GetRequiredService<IGameAuthService>();

    var currentAccessToken = GetExpiredApiAccessToken(playerId, _refreshTokenId);

    var client = uutApiApp.CreateClient();
    client.DefaultRequestHeaders.Add("Cookie", $"gameapi-refresh={currentRefreshToken}");

    using var content = JsonContent.Create(new
    {
      accessToken = currentAccessToken
    });

    var actualAuthTokenResponseMessage = await client.PostAsync("/api/user/refresh-token", content);

    var actualResponse = await actualAuthTokenResponseMessage.Content.ReadFromJsonAsync<Dictionary<string, object>>();
    Assert.NotNull(actualResponse);
    
    var actualNewAccessToken = Assert.Contains("accessToken", actualResponse);
    Assert.NotEqual(currentAccessToken, actualNewAccessToken);

    var actualCookiesFromResponse = actualAuthTokenResponseMessage.Headers
      .Where(header => header.Key == "Set-Cookie")
      .SelectMany(header => header.Value)
      .Select(cookie =>
      {
        var cookieParts = cookie.Split("=");
        return new
        {
          Name = cookieParts[0],
          Value = string.Join("=", cookieParts[1..])
        };
      })
      .ToDictionary(x => x.Name, x => x.Value);
    
    var actualNewRefreshCookieValue = Assert.Contains(GameAuthService.ApiRefreshTokenCookieName, actualCookiesFromResponse);
    Assert.DoesNotContain("current_refresh", actualNewRefreshCookieValue);
  }

  [Fact]
  public async Task WillReturn400WhenRefreshTokenCookieIsMissing()
  {
    var currentAccessToken = "token";

    await using var uutApiApp = GetApiFactory();

    var client = uutApiApp.CreateClient();

    var actualAuthTokenResponseMessage = await client.PostAsJsonAsync("/api/user/refresh-token", currentAccessToken);

    Assert.Equal(HttpStatusCode.BadRequest, actualAuthTokenResponseMessage.StatusCode);
  }

  [Fact]
  public async Task WillReturn400WhenAccessTokenIsMissing()
  {
    await using var uutApiApp = GetApiFactory();

    var client = uutApiApp.CreateClient();
    client.DefaultRequestHeaders.Add("Cookie", $"gameapi-refresh=refresh_token_value");

    var actualAuthTokenResponseMessage = await client.PostAsJsonAsync("/api/user/refresh-token", string.Empty);

    Assert.Equal(HttpStatusCode.BadRequest, actualAuthTokenResponseMessage.StatusCode);
  }

  [Fact]
  public async Task WillReturn400WhenAccessTokenIsInvalid()
  {
    await using var uutApiApp = GetApiFactory();

    var client = uutApiApp.CreateClient();
    client.DefaultRequestHeaders.Add("Cookie", $"gameapi-refresh=refresh_token_value");

    var actualAuthTokenResponseMessage = await client.PostAsJsonAsync("/api/user/refresh-token", string.Empty);

    Assert.Equal(HttpStatusCode.BadRequest, actualAuthTokenResponseMessage.StatusCode);
  }

  [Fact]
  public async Task WillProcessEventMessageInBackgroundWorker()
  {
    var testMessage = new TestApiMessage();

    var invoked = new TaskCompletionSource();
    var testHandler = Substitute.For<IDomainMessageHandler<TestApiMessage>>();
    testHandler
      .Handle(testMessage, Arg.Any<CancellationToken>())
      .Returns(_ => Task.Run(invoked.SetResult));

    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddScoped(_ => testHandler);
    });

    var eventBus = uutApiApp.Services.GetRequiredService<IEventBus>();

    await eventBus.PublishAsync(testMessage, CancellationToken.None);

    await invoked.Task.WaitAsync(TimeSpan.FromSeconds(1));

    await testHandler
      .Received(1)
      .Handle(testMessage, Arg.Any<CancellationToken>());
  }

  private WebApplicationFactory<Program> GetApiFactory(Action<IServiceCollection>? registerServices = null) => new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
      builder.ConfigureLogging(logBuilder =>
      {
        logBuilder.AddDebug();
        logBuilder.SetMinimumLevel(LogLevel.Information);
      });

      builder.ConfigureServices(services =>
      {
        // overwrite dbcontext with in-memory provider to eliminate direct sql server dependency.
        // the goal of these tests is to cover API pipeline implementations, not the business logic.
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDbContextOptionsConfiguration<GameDbContext>));
        if (descriptor != null)
        {
          services.Remove(descriptor);
        }

        services.AddDbContext<GameDbContext>(options =>
        {
          options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug()));
          options.EnableSensitiveDataLogging(true);
          options.UseInMemoryDatabase("TestGameDb");
          options.ConfigureWarnings(warns =>
          {
            warns.Ignore(InMemoryEventId.TransactionIgnoredWarning);
          });
        });

        registerServices?.Invoke(services);
      });

      builder.UseSetting("ConnectionStrings:GameDB", "test connection string");
      builder.UseSetting("Auth:Api:JwtSecret", _testJwtSecret);
      builder.UseSetting("Auth:Api:JwtAudience", _testJwtAudience);
      builder.UseSetting("Auth:Api:JwtTokenExpirationMin", $"{_testJwtExpirationMin}");
    });

  private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> apiAppFactory, long playerId, string refreshTokenId)
  {
    var scope = apiAppFactory.Services.CreateScope();

    var gameAuthService = scope.ServiceProvider.GetRequiredService<IGameAuthService>();

    var authToken = gameAuthService.GenerateApiJwtToken("test provider",
      "test provider user id",
      playerId,
      playerId,
      refreshTokenId);

    var httpClient = apiAppFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", authToken);

    return httpClient;
  }

  private static string GetExpiredApiAccessToken(long playerId, string refreshTokenId)
  {
    var claims = GameAuthService.CreateAccessTokenClaims("test provider",
      "test provider user id",
      playerId,
      playerId,
      refreshTokenId);

    var jwtToken = new JwtSecurityToken(
      claims: claims,
      // expiration must account for clock skew to be trully expired
      notBefore: DateTime.UtcNow.AddMinutes(-20),
      expires: DateTime.UtcNow.AddMinutes(-19),
      issuer: _testJwtAudience,
      audience: _testJwtAudience,
      signingCredentials: new SigningCredentials(
        GameAuthService.GetAccessTokenSigningKey(_testJwtSecret),
        SecurityAlgorithms.HmacSha256Signature)
      );

    return new JwtSecurityTokenHandler().WriteToken(jwtToken);
  }

  public sealed record TestApiMessage() : IDomainEvent;
}
