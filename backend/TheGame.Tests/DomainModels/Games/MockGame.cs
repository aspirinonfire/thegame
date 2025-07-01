using TheGame.Domain.DomainModels.Games;
using TheGame.Domain.DomainModels.LicensePlates;
using TheGame.Domain.DomainModels.Players;

namespace TheGame.Tests.DomainModels.Games;

public class MockGame : Game
{
  public MockGame(long gameId,
    IEnumerable<GameLicensePlate>? licensePlates,
    Player createdBy,
    bool isActive,
    DateTimeOffset? endedOn = null,
    string name = "test game")
  {
    Id = gameId;

    _gameLicensePlates = licensePlates?.ToHashSet() ?? [];

    CreatedBy = createdBy;

    _gamePlayerInvites = []; 

    Name = name;

    IsActive = isActive;

    EndedOn = endedOn;
  }

  public void SetActiveFlag(bool newValue)
  {
    IsActive = newValue;
  }

  public void AddInvitedPlayer(Player player,
    Guid? inviteToken = null,
    GamePlayerInviteStatus? gamePlayerInviteStatus = null)
  {
    _gamePlayerInvites.Add(new MockGamePlayer(player, this, inviteToken, gamePlayerInviteStatus));
  }
}

public class MockGamePlayer : GamePlayer
{
  public MockGamePlayer(Player player,
    Game game,
    Guid? inviteToken,
    GamePlayerInviteStatus? gamePlayerInviteStatus)
  {
    Player = player;
    Game = game;
    InviteStatus = gamePlayerInviteStatus.GetValueOrDefault(GamePlayerInviteStatus.Accepted);
    InvitationToken = inviteToken.GetValueOrDefault(Guid.NewGuid());
  }
}
