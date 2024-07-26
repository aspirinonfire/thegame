using TheGame.Domain.DomainModels.Players;

namespace TheGame.Domain.DomainModels.PlayerIdentities;

public interface IPlayerIdentityFactory
{
  Maybe<PlayerIdentity> CreatePlayerIdentity(NewPlayerIdentityRequest newPlayerRequest);
}

public partial class PlayerIdentity
{
  public class PlayerIdentityFactory(GameDbContext dbContext, IPlayerFactory playerFactory) : IPlayerIdentityFactory
  {
    public Maybe<PlayerIdentity> CreatePlayerIdentity(NewPlayerIdentityRequest newPlayerRequest)
    {
      var playerIdentity = new PlayerIdentity()
      {
        ProviderIdentityId = newPlayerRequest.ProviderIdentityId,
        ProviderName = newPlayerRequest.ProviderName,
      };

      var newPlayerResult = playerFactory.CreateNewPlayer(newPlayerRequest.PlayerName);
      if (!newPlayerResult.TryGetSuccessful(out var newPlayer, out var newPlayerFailure))
      {
        return newPlayerFailure;
      }

      playerIdentity.Player = newPlayer;

      dbContext.PlayerIdentities.Add(playerIdentity);

      return playerIdentity;
    }
  }
}

public sealed record NewPlayerIdentityRequest(string ProviderName,
  string ProviderIdentityId,
  string PlayerName,
  ushort RefreshTokenByteCount,
  uint RefreshTokenAgeMinutes);
