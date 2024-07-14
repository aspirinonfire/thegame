using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OneOf;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.Utils;

namespace TheGame.Api.Auth;

public class GameAuthService(IHttpClientFactory httpClientFactory, IOptions<GameSettings> gameSettings)
{
  public const string PlayerIdentityAuthority = "iden_authority";
  public const string PlayerIdentityUserId = "iden_user_id";
  public const string PlayerIdentityIdClaimType = "player_identiy_id";
  public const string PlayerIdClaimType = "player_id";

  public const string UnsuccessfulExternalAuthError = "External auth result was not successful!";
  public const string MissingAuthClaimsError = "Failed to retrieve claims from external auth result!";
  public const string MissingAuthNameIdClaimError = "Failed to retrieve NameId claim from external auth result!";
  public const string MissingIdentityNameClaimError = "Failed to retrieve Player Name from external auth result!";

  public virtual async Task<OneOf<GoogleUserInfo, Failure>> GetGoogleIdentity(string accessToken)
  {
    if (string.IsNullOrEmpty(accessToken))
    {
      return new Failure("Access Token is missing.");
    }

    var httpClient = httpClientFactory.CreateClient();

    // Validate access token.
    var tokenInfoResponse = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v1/tokeninfo?access_token={accessToken}",
      HttpCompletionOption.ResponseHeadersRead);
    if (!tokenInfoResponse.IsSuccessStatusCode)
    {
      return new Failure("Failed to retrieve token info.");
    }

    var tokenInfo = await tokenInfoResponse.Content.ReadFromJsonAsync<GoogleTokenInfo>();
    if (gameSettings.Value.Auth.Google.ClientId != tokenInfo?.Audience)
    {
      return new Failure("Invalid Google Access Token audience.");
    }

    // Retrieve user info
    var userInfoResponse = await httpClient.GetAsync($"https://www.googleapis.com/oauth2/v3/userinfo?access_token={accessToken}",
      HttpCompletionOption.ResponseHeadersRead);
    if (!userInfoResponse.IsSuccessStatusCode)
    {
      return new Failure("Failed to retrieve user info.");
    }

    var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<GoogleUserInfo>();

    if (userInfo == null)
    {
      return new Failure("User info payload is empty.");
    }

    if (userInfo.Subject != tokenInfo.UserId)
    {
      return new Failure("Token user_id does not match user profile subject.");
    }

    return userInfo;
  }

  public virtual string GenerateJwtToken(GetOrCreatePlayerResult playerIdentity)
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
}
