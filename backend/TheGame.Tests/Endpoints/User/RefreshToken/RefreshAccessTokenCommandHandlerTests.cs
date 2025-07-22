using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.NSubstitute;
using TheGame.Api.Auth;
using TheGame.Api.Common;
using TheGame.Api.Endpoints.User.RefreshToken;
using TheGame.Domain.DomainModels;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Tests.DomainModels.Players;

namespace TheGame.Tests.Endpoints.User.RefreshToken;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]
public class RefreshAccessTokenCommandHandlerTests
{
  [Fact]
  public async Task WillRefreshTokensWhenInputIsValid()
  {
    var command = new RefreshAccessTokenCommand(
      "valid-access-token",
      "valid-refresh-token");

    var currentTimestamp = new DateTimeOffset(2025, 7, 22, 0, 0, 0, TimeSpan.Zero);

    var dbContext = Substitute.For<IGameDbContext>();
    dbContext
      .PlayerIdentities
      .Returns(_ => new PlayerIdentity[]
      {
        new MockPlayerIdentity(
          new MockPlayer(1, "test"),
          1,
          "test-provider",
          "test-id",
          false)
      }.AsQueryable().BuildMockDbSet());

    var gameAuthService = CreateGameAuthServiceMock(
      "new-access-token",
      new ApiAccessTokenPayload(
        1,
        "test-provider",
        "test-id",
        "refresh-token-id",
        false),
      new RefreshTokenPayload(
        "refresh-token-id",
        "nonce",
        currentTimestamp.AddHours(1).ToUnixTimeSeconds()),
      "new-refresh-id",
      "new-refresh-token",
      currentTimestamp.AddHours(1));

    var timeProvider = CommonMockedServices.GetMockedTimeProvider(currentTimestamp);

    var uutHandler = CreateUutCmdHandler(dbContext,
      gameAuthService,
      timeProvider);

    var actualCmdResult = await uutHandler.Execute(command, CancellationToken.None);

    var actualSuccess = AssertResult.AssertIsSucceessful(actualCmdResult);

    Assert.Equal("new-access-token", actualSuccess.AccessToken);
    Assert.Equal("new-refresh-token", actualSuccess.RefreshTokenValue);
    Assert.Equal(TimeSpan.FromHours(1), actualSuccess.RefreshTokenExpiresIn);

    await dbContext
      .Received(1)
      .SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WillNotRefreshTokensOnTokenIdMismatch()
  {
    var command = new RefreshAccessTokenCommand(
      "valid-access-token",
      "mismatch-id-refresh-token");

    var currentTimestamp = new DateTimeOffset(2025, 7, 22, 0, 0, 0, TimeSpan.Zero);

    var dbContext = Substitute.For<IGameDbContext>();

    var gameAuthService = CreateGameAuthServiceMock(
      "new-access-token",
      new ApiAccessTokenPayload(
        1,
        "test-provider",
        "test-id",
        "refresh-token-id",
        false),
      new RefreshTokenPayload(
        "mismatched-refresh-token-id",
        "nonce",
        currentTimestamp.AddHours(1).ToUnixTimeSeconds()),
      string.Empty,
      string.Empty,
      currentTimestamp.AddHours(1));

    var timeProvider = CommonMockedServices.GetMockedTimeProvider(currentTimestamp);

    var uutHandler = CreateUutCmdHandler(dbContext,
      gameAuthService,
      timeProvider);

    var actualCmdResult = await uutHandler.Execute(command, CancellationToken.None);

    AssertResult.AssertIsFailure(actualCmdResult);

    await dbContext
      .Received(0)
      .SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WillNotRefreshTokensOnExpiredRefreshToken()
  {
    var command = new RefreshAccessTokenCommand(
      "valid-access-token",
      "expired-refresh-token");

    var currentTimestamp = new DateTimeOffset(2025, 7, 22, 0, 0, 0, TimeSpan.Zero);

    var dbContext = Substitute.For<IGameDbContext>();

    var gameAuthService = CreateGameAuthServiceMock(
      "new-access-token",
      new ApiAccessTokenPayload(
        1,
        "test-provider",
        "test-id",
        "refresh-token-id",
        false),
      new RefreshTokenPayload(
        "refresh-token-id",
        "nonce",
        currentTimestamp.AddHours(-1).ToUnixTimeSeconds()),
      string.Empty,
      string.Empty,
      currentTimestamp.AddHours(1));

    var timeProvider = CommonMockedServices.GetMockedTimeProvider(currentTimestamp);

    var uutHandler = CreateUutCmdHandler(dbContext,
      gameAuthService,
      timeProvider);

    var actualCmdResult = await uutHandler.Execute(command, CancellationToken.None);

    AssertResult.AssertIsFailure(actualCmdResult);

    await dbContext
      .Received(0)
      .SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WillNotRefreshTokensOnInvalidAccessToken()
  {
    var command = new RefreshAccessTokenCommand(
      "invalid-access-token",
      RefreshToken: "valid-refresh-token");

    var currentTimestamp = new DateTimeOffset(2025, 7, 22, 0, 0, 0, TimeSpan.Zero);

    var dbContext = Substitute.For<IGameDbContext>();

    var gameAuthService = CreateGameAuthServiceMock(
      "new-access-token",
      new Failure("invalid"),
      default!,
      string.Empty,
      string.Empty,
      currentTimestamp.AddHours(1));

    var timeProvider = CommonMockedServices.GetMockedTimeProvider(currentTimestamp);

    var uutHandler = CreateUutCmdHandler(dbContext,
      gameAuthService,
      timeProvider);

    var actualCmdResult = await uutHandler.Execute(command, CancellationToken.None);

    AssertResult.AssertIsFailure(actualCmdResult);

    await dbContext
      .Received(0)
      .SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  [Fact]
  public async Task WillNotRefreshTokensOnDisabledIdentity()
  {
    var command = new RefreshAccessTokenCommand(
      "disabled-identity-access-token",
      RefreshToken: "valid-refresh-token");

    var currentTimestamp = new DateTimeOffset(2025, 7, 22, 0, 0, 0, TimeSpan.Zero);

    var dbContext = Substitute.For<IGameDbContext>();
    dbContext
      .PlayerIdentities
      .Returns(_ => new PlayerIdentity[]
      {
        new MockPlayerIdentity(
          new MockPlayer(1, "test"),
          1,
          "test-provider",
          "test-id",
          true)
      }.AsQueryable().BuildMockDbSet());

    var gameAuthService = CreateGameAuthServiceMock(
      "new-access-token",
      new ApiAccessTokenPayload(
        1,
        "test-provider",
        "test-id",
        "refresh-token-id",
        false),
      new RefreshTokenPayload(
        "refresh-token-id",
        "nonce",
        currentTimestamp.AddHours(1).ToUnixTimeSeconds()),
      string.Empty,
      string.Empty,
      currentTimestamp.AddHours(1));

    var timeProvider = CommonMockedServices.GetMockedTimeProvider(currentTimestamp);

    var uutHandler = CreateUutCmdHandler(dbContext,
      gameAuthService,
      timeProvider);

    var actualCmdResult = await uutHandler.Execute(command, CancellationToken.None);

    AssertResult.AssertIsFailure(actualCmdResult);

    await dbContext
      .Received(0)
      .SaveChangesAsync(Arg.Any<CancellationToken>());
  }

  private static ICommandHandler<RefreshAccessTokenCommand, RefreshAccessTokenCommand.Result> CreateUutCmdHandler(
    IGameDbContext gameDbContext,
    IGameAuthService gameAuthService,
    TimeProvider timeProvider)
  {
    return new RefreshAccessTokenCommandHandler(gameDbContext,
      gameAuthService,
      timeProvider,
      CommonMockedServices.CreatePassthroughExecutionWrapper(),
      NullLogger<RefreshAccessTokenCommandHandler>.Instance);
  }

  private static IGameAuthService CreateGameAuthServiceMock(string newApiToken,
    Result<ApiAccessTokenPayload> accessTokenPayload,
    Result<RefreshTokenPayload> refreshTokenPayload,
    string newRefreshTokenId,
    string newRefreshTokenValue,
    DateTimeOffset newRefreshTokenExpiration)
  {
    var gameAuthService = Substitute.For<IGameAuthService>();
    gameAuthService
      .GenerateRefreshToken()
      .Returns(new NewRefreshToken(newRefreshTokenId, newRefreshTokenValue, newRefreshTokenExpiration.ToUnixTimeSeconds()));

    gameAuthService
      .GenerateApiJwtToken(Arg.Any<string>(),
        Arg.Any<string>(),
        Arg.Any<long>(),
        Arg.Any<long>(),
        Arg.Any<string>())
      .Returns(newApiToken);

    gameAuthService
      .GetAccessTokenPayload(Arg.Any<string>())
      .Returns(accessTokenPayload);

    gameAuthService
      .ExtractRefreshTokenPayload(Arg.Any<string>())
      .Returns(refreshTokenPayload);

    return gameAuthService;
  }
}
