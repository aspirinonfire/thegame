﻿using Google.Apis.Auth;
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
  public async Task WillAuthenticateNewPlayerWithWhenGoogleTokenIdIsValid()
  {
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
            "refresh_token"));

        return mediatr;
      });

      services.AddScoped(sp =>
      {
        var opts = sp.GetRequiredService<IOptions<GameSettings>>();
        var mediatr = sp.GetRequiredService<IMediator>();

        var mockedAuthService = Substitute.ForPartsOf<GameAuthService>(NullLogger<GameAuthService>.Instance,
          mediatr,
          opts);

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

    var actualAuthTokenResponseMessage = await client.PostAsJsonAsync("/api/user/google/apitoken", testGoogleIdToken);

    Assert.True(actualAuthTokenResponseMessage.IsSuccessStatusCode);

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

    var actualResponse = await actualAuthTokenResponseMessage.Content.ReadFromJsonAsync<Dictionary<string, string>>();
    Assert.NotNull(actualResponse);
    var actualToken = Assert.Contains("accessToken", actualResponse);
    Assert.NotEmpty(actualToken);
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
    });

  private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> apiAppFactory, long playerId)
  {
    var playerIdentity = new GetOrCreatePlayerResult(false,
      playerId,
      playerId,
      "test provider",
      "test provider user id",
      "refresh_token");

    var scope = apiAppFactory.Services.CreateScope();

    var gameAuthService = scope.ServiceProvider.GetRequiredService<GameAuthService>();

    var authToken = gameAuthService.GenerateApiJwtToken(playerIdentity);

    var httpClient = apiAppFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", authToken);

    return httpClient;
  }
}
