using System.Text.Json.Serialization;
using TheGame.Domain.CommandHandlers;
using TheGame.Domain.DomainModels.PlayerIdentities;

namespace TheGame.Api.Auth;

public sealed record GoogleUserInfo
{
  [JsonPropertyName("sub")]
  public required string Subject { get; init; }
  
  [JsonPropertyName("name")]
  public required string Name { get; init; }
  
  [JsonPropertyName("email")]
  public required string Email { get; set; }
  
  [JsonPropertyName("picture")]
  public required string PictureUrl { get; init; }

  public GetOrCreateNewPlayerCommand ToGetOrCreateNewPlayerCommand()
  {
    var request = new NewPlayerIdentityRequest("Google",
    Subject,
    string.Empty,
    Name);

    return new GetOrCreateNewPlayerCommand(request);
  }
}

public sealed record GoogleTokenInfo
{
  [JsonPropertyName("audience")]
  public required string Audience { get; init; }

  [JsonPropertyName("user_id")]
  public required string UserId { get; init; }
}
