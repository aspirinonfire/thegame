using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MockQueryable.NSubstitute;
using NSubstitute.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TheGame.Api;
using TheGame.Api.Auth;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Tests;

[Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
public class ApiRoutesTests
{
  private const string _testJwtSecret = "this is a jwt secret value for testing api routes!";
  private const string _testJwtAudience = "test audience";
  private const ushort _testJwtExpirationMin = 1;

  [Fact]
  public async Task CanRunHealthCheckRouteWithoutAuthentication()
  {
    await using var uutApiApp = GetApiFactory();
    var client = uutApiApp.CreateClient();

    var actualApiResponse = await client.GetAsync("/api/health");

    Assert.Equal(HttpStatusCode.OK, actualApiResponse.StatusCode);

    var actualResponseBody = await actualApiResponse.Content.ReadAsStringAsync();
    Assert.Equal("Healthy", actualResponseBody);
  }

  [Fact]
  public async Task WillReturn401ForUnauthenticatedUserWhenAccessingApiRoutes()
  {
    await using var uutApiApp = GetApiFactory();
    var client = uutApiApp.CreateClient();

    var actualApiResponse = await client.GetAsync("/api/user");

    Assert.Equal(HttpStatusCode.Unauthorized, actualApiResponse.StatusCode);
  }

  [Fact]
  public async Task CanGetPlayerInfoForAuthenticatedUser()
  {
    var testPlayerId = 123;
    var expectedTestPlayerInfo = new PlayerInfo("Test Player", testPlayerId);

    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddScoped(sp =>
      {
        var mockQueryProvider = Substitute.For<IPlayerQueryProvider>();
        mockQueryProvider
          .GetPlayerInfoQuery(testPlayerId)
          .Returns(new[] { expectedTestPlayerInfo }.BuildMock());

        return mockQueryProvider;
      });
    });

    var client = CreateAuthenticatedClient(uutApiApp, testPlayerId);

    var actualApiResponse = await client.GetAsync("/api/user");

    Assert.Equal(HttpStatusCode.OK, actualApiResponse.StatusCode);

    var actualPlayerInfo = await actualApiResponse.Content.ReadFromJsonAsync<PlayerInfo>();
    Assert.Equal(expectedTestPlayerInfo, actualPlayerInfo);
  }

  [Fact]
  public async Task WillAuthenticateNewPlayerWithWhenGoogleAuthCodeIsValid()
  {
    var authCode = "auth-code";

    var testGoogleIdToken = "google-id-token";

    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddTransient(sp =>
      {
        var mediatr = Substitute.For<IMediator>();
        mediatr
          .Send(Arg.Any<GetOrCreateNewPlayerCommand>())
          .Returns(new GetOrCreatePlayerResult(true,
            123,
            123,
            "test provider",
            "test id",
            "refresh_token",
            new DateTimeOffset(2024, 1, 13, 0, 0, 0, TimeSpan.Zero)));

        return mediatr;
      });

      services.AddScoped(sp =>
      {
        var opts = sp.GetRequiredService<IOptions<GameSettings>>();
        var mediatr = sp.GetRequiredService<IMediator>();
        var systemService = sp.GetRequiredService<ISystemService>();

        var mockedAuthService = Substitute.ForPartsOf<GameAuthService>(NullLogger<GameAuthService>.Instance,
          mediatr,
          systemService,
          opts);

        mockedAuthService
          .Configure()
          .ExchangeGoogleAuthCodeForTokens(authCode)
          .Returns(new TokenResponse()
          {
            IdToken = testGoogleIdToken
          });

        mockedAuthService
          .Configure()
          .GetValidatedGoogleIdTokenPayload(testGoogleIdToken)
          .Returns(new GoogleJsonWebSignature.Payload()
          {
            Subject = "test-user",
            Name = "Test User"
          });

        return mockedAuthService;
      });
    });

    var client = uutApiApp.CreateClient();

    var actualAuthTokenResponseMessage = await client.PostAsJsonAsync("/api/user/google/apitoken", authCode);

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
    Assert.Contains("samesite=strict", actualRefreshCookieValue);
    Assert.Contains("httponly", actualRefreshCookieValue);
    Assert.Contains("path=/api/user/refresh-token", actualRefreshCookieValue);

    var actualResponse = await actualAuthTokenResponseMessage.Content.ReadFromJsonAsync<Dictionary<string, string>>();
    Assert.NotNull(actualResponse);
    var actualToken = Assert.Contains("accessToken", actualResponse);
    Assert.NotEmpty(actualToken);
  }

  [Fact]
  public async Task WillRefreshTokenWithValidAccessToken()
  {
    var currentRefreshToken = "current_refresh";
    var playerId = 123L;

    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddTransient(sp =>
      {
        var mediatr = Substitute.For<IMediator>();
        mediatr
          .Send(Arg.Any<RotatePlayerIdentityRefreshTokenCommand>())
          .Returns(new RotatePlayerIdentityRefreshTokenResult(
            "new_refresh_token",
            new DateTimeOffset(2024, 1, 13, 0, 0, 0, TimeSpan.Zero),
            playerId,
            playerId,
            "test provider",
            "test provider ident id"));

        return mediatr;
      });
    });

    await using var scope = uutApiApp.Services.CreateAsyncScope();

    var gameAuthService = scope.ServiceProvider.GetRequiredService<GameAuthService>();
    
    var currentAccessToken = gameAuthService.GenerateApiJwtToken("test provider",
      "test provider user id",
      playerId,
      playerId);

    var client = uutApiApp.CreateClient();
    client.DefaultRequestHeaders.Add("Cookie", $"gameapi-refresh={currentRefreshToken}");

    var actualAuthTokenResponseMessage = await client.PostAsJsonAsync("/api/user/refresh-token", currentAccessToken);

    Assert.Equal(HttpStatusCode.OK, actualAuthTokenResponseMessage.StatusCode);

    var actualResponse = await actualAuthTokenResponseMessage.Content.ReadFromJsonAsync<Dictionary<string, string>>();
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

  private WebApplicationFactory<Program> GetApiFactory(Action<IServiceCollection>? registerServices = null) => new WebApplicationFactory<Program>()
    .WithWebHostBuilder(builder =>
    {
      builder.ConfigureServices(services =>
      {
        // overwrite dbcontext with in-memory provider to eliminate direct sql server dependency.
        // the goal of these tests is to cover API pipeline implementations, not the business logic.
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<GameDbContext>));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }

        services.AddDbContext<GameDbContext>(options =>
        {
          options.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddDebug()));
          options.EnableSensitiveDataLogging(true);
          options.UseLazyLoadingProxies(true);
          options.UseInMemoryDatabase("TestGameDb");
        });

        registerServices?.Invoke(services);
      });

      builder.UseSetting("ConnectionStrings:GameDB", "test connection string");
      builder.UseSetting("Auth:Api:JwtSecret", _testJwtSecret);
      builder.UseSetting("Auth:Api:JwtAudience", _testJwtAudience);
      builder.UseSetting("Auth:Api:JwtTokenExpirationMin", $"{_testJwtExpirationMin}");
    });

  private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> apiAppFactory, long playerId)
  {
    var scope = apiAppFactory.Services.CreateScope();

    var gameAuthService = scope.ServiceProvider.GetRequiredService<GameAuthService>();

    var authToken = gameAuthService.GenerateApiJwtToken("test provider",
      "test provider user id",
      playerId,
      playerId);

    var httpClient = apiAppFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", authToken);

    return httpClient;
  }
}
