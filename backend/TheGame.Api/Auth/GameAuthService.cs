using Google.Apis.Auth;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneOf;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public sealed record ApiTokens(bool IsNewIdentity, string AccessToken);

public class GameAuthService(ILogger<GameAuthService> logger, IMediator mediatr, IOptions<GameSettings> gameSettings)
{
  public const string ApiRefreshTokenCookieName = "gameapi-refresh";

  public const string PlayerIdentityAuthority = "iden_authority";
  public const string PlayerIdentityUserId = "iden_user_id";
  public const string PlayerIdentityIdClaimType = "player_identiy_id";
  public const string PlayerIdClaimType = "player_id";
  
  public const string MissingIdTokenError = "missing_id_token";
  public const string InvalidIdTokenError = "invalid_id_token";
  public const string GeneralErrorWhileValidatingTokenError = "token_validation_general_error";

  /// <summary>
  /// Use valid Google ID Token to obtain player identity and generate API auth and refresh tokens.
  /// </summary>
  /// <remarks>
  /// This method expects ID Token since its payload contains everything required to generate valid player identity.
  /// Currently, no additional user information is required so Access Token is not needed.
  /// If that requirement changes, UI will need to provide authorization code so auth method can obtain necessary tokens and make appropriate requests.
  /// </remarks>
  /// <param name="googleIdToken"></param>
  /// <param name="httpContext"></param>
  /// <returns></returns>
  public async Task<OneOf<ApiTokens, Failure>> AuthenticateWithGoogleIdToken(string googleIdToken, HttpContext httpContext)
  {
    var googleIdentityResult = await GetValidatedGoogleIdTokenPayload(googleIdToken);
    if (!googleIdentityResult.TryGetSuccessful(out var idTokenPayload, out var tokenValidationFailure))
    {
      return tokenValidationFailure;
    }

    var identityRequest = new NewPlayerIdentityRequest("Google",
      idTokenPayload.Subject,
      GenerateRefreshToken(),
      idTokenPayload.Name);

    var getOrCreatePlayerCommand = new GetOrCreateNewPlayerCommand(identityRequest);
    var getOrCreatePlayerResult = await mediatr.Send(getOrCreatePlayerCommand);
    if (!getOrCreatePlayerResult.TryGetSuccessful(out var playerIdentity, out var commandFailure))
    {
      return commandFailure;
    }

    // TODO store refresh token expiration in DB as well. Rotate if necessary
    if (!string.IsNullOrEmpty(playerIdentity.RefreshToken))
    {
      //var cookieDomain = httpContext.Request.Headers.Origin
      //  .FirstOrDefault($"{httpContext.Request.Scheme}:{httpContext.Request.Host.Value}");

      // TODO set domain from ctx (must omit scheme and port)
      var cookieDomain = "localhost";

      var refreshCookieOptions = new CookieBuilder()
      {
        Name = ApiRefreshTokenCookieName,
        IsEssential = true,
        HttpOnly = true,
        Domain = cookieDomain,
        Path = "/",
        MaxAge = TimeSpan.FromDays(7),
        Expiration = TimeSpan.FromDays(7),
        SameSite = SameSiteMode.Strict,
        SecurePolicy = CookieSecurePolicy.Always
      }.Build(httpContext);

      httpContext.Response.Cookies.Append(ApiRefreshTokenCookieName, playerIdentity.RefreshToken, refreshCookieOptions);
    }
    
    var apiToken = GenerateApiJwtToken(playerIdentity);

    return new ApiTokens(playerIdentity.IsNewIdentity, apiToken);
  }

  /// <summary>
  /// Validate Google ID Token and return payload claims. For validation rules see <see href="https://developers.google.com/identity/openid-connect/openid-connect#validatinganidtoken"/>
  /// </summary>
  /// <param name="googleIdToken"></param>
  /// <returns></returns>
  public virtual async Task<OneOf<GoogleJsonWebSignature.Payload, Failure>> GetValidatedGoogleIdTokenPayload(string googleIdToken)
  {
    if (string.IsNullOrEmpty(googleIdToken))
    {
      return new Failure(MissingIdTokenError);
    }

    try
    {
      var tokenValidationSettings = new GoogleJsonWebSignature.ValidationSettings()
      {
        Audience = [gameSettings.Value.Auth.Google.ClientId]
      };

      return await GoogleJsonWebSignature.ValidateAsync(googleIdToken, tokenValidationSettings);
    }
    catch (InvalidJwtException invalidIdTokenException)
    {
      logger.LogError(invalidIdTokenException, "Google ID Token is invalid.");
      return new Failure(InvalidIdTokenError);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Encountered error while validating Google ID Token");
      return new Failure(GeneralErrorWhileValidatingTokenError);
    }
  }

  /// <summary>
  /// Generate API token using JWT format. This token is expected to be used during Game API authentication.
  /// </summary>
  /// <param name="playerIdentity"></param>
  /// <returns></returns>
  public virtual string GenerateApiJwtToken(GetOrCreatePlayerResult playerIdentity)
  {
    var claims = new List<Claim>
    {
      new(PlayerIdentityAuthority, playerIdentity.ProviderName, ClaimValueTypes.String),
      new(PlayerIdentityUserId, playerIdentity.ProviderIdentityId, ClaimValueTypes.String),
      new(PlayerIdClaimType, $"{playerIdentity.PlayerId}", ClaimValueTypes.String),
      new(PlayerIdentityIdClaimType, $"{playerIdentity.PlayerIdentityId}", ClaimValueTypes.String),
    };

    var signingKey = Encoding.UTF8.GetBytes(gameSettings.Value.Auth.Api.JwtSecret);

    var jwtToken = new JwtSecurityToken(
      claims: claims,
      notBefore: DateTime.UtcNow,
      expires: DateTime.UtcNow.AddMinutes(60),
      audience: gameSettings.Value.Auth.Api.JwtAudience,
      signingCredentials: new SigningCredentials(
        new SymmetricSecurityKey(signingKey),
        SecurityAlgorithms.HmacSha256Signature)
      );
    
    return new JwtSecurityTokenHandler().WriteToken(jwtToken);
  }

  public virtual string GenerateRefreshToken()
  {
    var randomBytes = RandomNumberGenerator.GetBytes(128);
    return Convert.ToBase64String(randomBytes);
  }
}
