using TheGame.Domain.DomainModels.PlayerIdentities;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Tests.DomainModels.Players;

public class MockPlayerIdentity : PlayerIdentity
{
  public MockPlayerIdentity() { }

  public MockPlayerIdentity(Player player,
    long playerIdentityId,
    string identityProviderName,
    string providerIdentityId,
    bool isDisabled)
  {
    Id = playerIdentityId;
    ProviderIdentityId = providerIdentityId;
    ProviderName = identityProviderName;
    IsDisabled = isDisabled;
    Player = player;
  }
}