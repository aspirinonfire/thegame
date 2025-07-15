using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using TheGame.Api;
using TheGame.Api.Auth;

namespace TheGame.Tests.Auth;

[Trait(XunitTestProvider.Category, XunitTestProvider.Unit)]

public class GameAuthServiceTests
{
  private static GameSettings _validGameSettings = new()
  {
    Auth = new AuthSettings()
    {
      Api = new ApiAuthSettings()
      {
        JwtAudience = "test_aud",
        JwtSecret = "my_very_very_long_test_secret_123!",
        RefreshTokenAgeMinutes = 5,
        RefreshTokenByteCount = 64,
        JwtTokenExpirationMin = 1
      },
      Google = new ApiGoogleSettings()
      {
        ClientId = "<client_id>",
        ClientSecret = "<client_secret>"
      }
    }
  };

  [Fact]
  public void CanGenerateJwtToken()
  {
    var options = Substitute.For<IOptions<GameSettings>>();
    options.Value.Returns(_validGameSettings);

    var uutAuthService = new GameAuthService(NullLogger<GameAuthService>.Instance,
      options);

    var actualToken = uutAuthService.GenerateApiJwtToken("provider name",
      "provider identity id",
    13,
    12);

    Assert.NotNull(actualToken);
  }

  [Fact]
  public void WillValidateJwtTokenWithValidParams()
  {
    var testToken = new JwtSecurityToken(
      claims: [],
      notBefore: new DateTime(2024, 1, 13, 0, 0, 0, DateTimeKind.Utc),
      issuer: GameAuthService.ValidApiTokenIssuer,
      expires: DateTime.UtcNow.AddMinutes(1),
      audience: _validGameSettings.Auth.Api.JwtAudience,
      signingCredentials: new SigningCredentials(
        GameAuthService.GetAccessTokenSigningKey(_validGameSettings.Auth.Api.JwtSecret),
        SecurityAlgorithms.HmacSha256Signature)
      );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(testToken);

    var tokenHandler = new JwtSecurityTokenHandler();

    var uutValidationParams = GameAuthService.GetTokenValidationParams(_validGameSettings.Auth.Api.JwtAudience,
      _validGameSettings.Auth.Api.JwtSecret,
      GameAuthService.ValidApiTokenIssuer);

    var actualTokenClaims = tokenHandler.ValidateToken(tokenString, uutValidationParams, out _);
    Assert.NotNull(actualTokenClaims);
  }

  [Fact]
  public void WillValidateJwtTokenWithExpiredLifetimeAndOverride()
  {
    var testToken = new JwtSecurityToken(
      claims: [],
      notBefore: new DateTime(2024, 1, 13, 0, 0, 0, DateTimeKind.Utc),
      issuer: GameAuthService.ValidApiTokenIssuer,
      expires: DateTime.UtcNow.AddHours(-1),
      audience: _validGameSettings.Auth.Api.JwtAudience,
      signingCredentials: new SigningCredentials(
        GameAuthService.GetAccessTokenSigningKey(_validGameSettings.Auth.Api.JwtSecret),
        SecurityAlgorithms.HmacSha256Signature)
      );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(testToken);

    var tokenHandler = new JwtSecurityTokenHandler();

    var uutValidationParams = GameAuthService.GetTokenValidationParams(_validGameSettings.Auth.Api.JwtAudience,
      _validGameSettings.Auth.Api.JwtSecret,
      GameAuthService.ValidApiTokenIssuer);
    uutValidationParams.ValidateLifetime = false;

    var actualTokenClaims = tokenHandler.ValidateToken(tokenString, uutValidationParams, out _);
    Assert.NotNull(actualTokenClaims);
  }

  [Fact]
  public void WillInvalidateJwtTokenWithExpiredLifetime()
  {
    var testToken = new JwtSecurityToken(
      claims: [],
      notBefore: new DateTime(2024, 1, 13, 0, 0, 0, DateTimeKind.Utc),
      expires: DateTime.UtcNow.AddHours(-1),
      audience: _validGameSettings.Auth.Api.JwtAudience,
      signingCredentials: new SigningCredentials(
        GameAuthService.GetAccessTokenSigningKey(_validGameSettings.Auth.Api.JwtSecret),
        SecurityAlgorithms.HmacSha256Signature)
      );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(testToken);

    var tokenHandler = new JwtSecurityTokenHandler();

    var uutValidationParams = GameAuthService.GetTokenValidationParams(_validGameSettings.Auth.Api.JwtAudience,
      _validGameSettings.Auth.Api.JwtSecret,
      GameAuthService.ValidApiTokenIssuer);

    Assert.Throws<SecurityTokenExpiredException>(() => tokenHandler.ValidateToken(tokenString, uutValidationParams, out _));
  }

  [Fact]
  public void WillInvalidateJwtTokenWithInvalidAudience()
  {
    var testToken = new JwtSecurityToken(
      claims: [],
      notBefore: new DateTime(2024, 1, 13, 0, 0, 0, DateTimeKind.Utc),
      expires: DateTime.UtcNow.AddMinutes(1),
      audience: "this is wrong audience",
      signingCredentials: new SigningCredentials(
        GameAuthService.GetAccessTokenSigningKey(_validGameSettings.Auth.Api.JwtSecret),
        SecurityAlgorithms.HmacSha256Signature)
      );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(testToken);

    var tokenHandler = new JwtSecurityTokenHandler();

    var uutValidationParams = GameAuthService.GetTokenValidationParams(_validGameSettings.Auth.Api.JwtAudience,
      _validGameSettings.Auth.Api.JwtSecret,
      GameAuthService.ValidApiTokenIssuer);

    Assert.Throws<SecurityTokenInvalidAudienceException>(() => tokenHandler.ValidateToken(tokenString, uutValidationParams, out _));
  }

  [Fact]
  public void WillInvalidateJwtTokenWithInvalidSigningKey()
  {
    var testToken = new JwtSecurityToken(
      claims: [],
      notBefore: new DateTime(2024, 1, 13, 0, 0, 0, DateTimeKind.Utc),
      expires: DateTime.UtcNow.AddMinutes(1),
      audience: _validGameSettings.Auth.Api.JwtAudience,
      signingCredentials: new SigningCredentials(
        GameAuthService.GetAccessTokenSigningKey("this jwt secret key is invalid and should fail the test!"),
        SecurityAlgorithms.HmacSha256Signature)
      );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(testToken);

    var tokenHandler = new JwtSecurityTokenHandler();

    var uutValidationParams = GameAuthService.GetTokenValidationParams(_validGameSettings.Auth.Api.JwtAudience,
      _validGameSettings.Auth.Api.JwtSecret,
      GameAuthService.ValidApiTokenIssuer);

    Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() => tokenHandler.ValidateToken(tokenString, uutValidationParams, out _));
  }
}
