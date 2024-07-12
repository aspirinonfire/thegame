using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.NSubstitute;
using OneOf.Types;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using TheGame.Api;
using TheGame.Api.Auth;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Tests;

[Trait(XunitTestProvider.Category, XunitTestProvider.Integration)]
public class ApiRoutesTests
{
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
  public async Task WillStopApiExecutionWhenMissingXsrfToken()
  {
    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddSingleton(sp =>
      {
        var mockedMediatr = Substitute.For<IMediator>();

        mockedMediatr
          .Send(Arg.Any<EndGameCommand>())
          .Returns(new Success());

        return mockedMediatr;
      });
    });

    var client = CreateAuthenticatedClient(uutApiApp, 123);

    var actualApiResponse = await client.PostAsync("/api/game/1/endgame", null);

    Assert.Equal(HttpStatusCode.BadRequest, actualApiResponse.StatusCode);
  }

  [Fact]
  public async Task WillStopApiExecutionWhenXsrfTokenIsInvalid()
  {
    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddSingleton(sp =>
      {
        var mockedMediatr = Substitute.For<IMediator>();

        mockedMediatr
          .Send(Arg.Any<EndGameCommand>())
          .Returns(new Success());

        return mockedMediatr;
      });
    });

    var client = CreateAuthenticatedClient(uutApiApp, 123);
    client.DefaultRequestHeaders.TryAddWithoutValidation("X-XSRF-TOKEN", "invalid_token");

    var actualApiResponse = await client.PostAsync("/api/game/1/endgame", null);

    Assert.Equal(HttpStatusCode.BadRequest, actualApiResponse.StatusCode);
  }

  [Fact]
  public async Task WillSuccessfullyExecuteApiRequestWithValidXsrfToken()
  {
    await using var uutApiApp = GetApiFactory(services =>
    {
      services.AddSingleton(sp =>
      {
        var mockedMediatr = Substitute.For<IMediator>();

        mockedMediatr
          .Send(Arg.Any<EndGameCommand>())
          .Returns(new Success());

        return mockedMediatr;
      });
    });

    var client = CreateAuthenticatedClient(uutApiApp, 123);
    
    await AddXsrfTokenAndCookie(client);

    var actualApiResponse = await client.PostAsync("/api/game/1/endgame", null);

    Assert.Equal(HttpStatusCode.OK, actualApiResponse.StatusCode);
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
    });

  private static HttpClient CreateAuthenticatedClient(WebApplicationFactory<Program> apiAppFactory, long playerId)
  {
    // Create claims and claims principal
    var claims = new List<Claim>
    {
      new(GameAuthService.PlayerIdClaimType, $"{playerId}", ClaimValueTypes.String),
      new(GameAuthService.PlayerIdentityIdClaimType, $"{playerId}", ClaimValueTypes.String),
    };

    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

    // Create auth ticket based on the claims principal
    var authProperties = new AuthenticationProperties
    {
      AllowRefresh = false,
      ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(2),
      IsPersistent = false,
      IssuedUtc = DateTimeOffset.UtcNow
    };

    var authTicket = new AuthenticationTicket(claimsPrincipal, authProperties, CookieAuthenticationDefaults.AuthenticationScheme);

    // create cookie string from auth ticket
    var dataProtector = apiAppFactory.Services
      .GetRequiredService<IDataProtectionProvider>()
      .CreateProtector("Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationMiddleware",
        CookieAuthenticationDefaults.AuthenticationScheme,
        "v2");
    
    var ticketDataFormat = new TicketDataFormat(dataProtector);
    var cookieValue = ticketDataFormat.Protect(authTicket);

    // add auth cookie to client headers
    var httpClient = apiAppFactory.CreateClient();
    httpClient.DefaultRequestHeaders.Add("Cookie", $"{GameAuthenticationServiceExtensions.AuthCookieName}={cookieValue}");

    return httpClient;
  }

  private static async Task AddXsrfTokenAndCookie(HttpClient authenticatedHttpClient)
  {
    // get XSRF token from API
    var xsrfTokenResponse = await authenticatedHttpClient.GetAsync("/api/xsrftoken", HttpCompletionOption.ResponseHeadersRead);
    Assert.Equal(HttpStatusCode.OK, xsrfTokenResponse.StatusCode);

    var xsrfTokenLkp = await xsrfTokenResponse.Content.ReadFromJsonAsync<Dictionary<string, string>>();

    Assert.NotNull(xsrfTokenLkp);
    var actualToken = Assert.Contains("token", xsrfTokenLkp);

    // attach all response cookies to a next request. this step is required for Anti-Forgery to function properly
    var responseCookies = xsrfTokenResponse.Headers.GetValues("Set-Cookie")
      .Select(cookieString => cookieString.Split(";").First())
      .ToList()
      .AsReadOnly();

    authenticatedHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-XSRF-TOKEN", actualToken);
    authenticatedHttpClient.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", responseCookies);
  }
}
