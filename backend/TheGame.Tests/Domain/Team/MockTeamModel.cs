using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.Player;
using TheGame.Domain.DomainModels.Team;

namespace TheGame.Tests.Domain.Team
{
  internal class MockTeamModel : TeamModel
  {
    // TODO replace with navigation props in base model!
    public IEnumerable<GameModel> Games => _games;
    public IEnumerable<PlayerModel> Player => _players;

    public MockTeamModel(IEnumerable<PlayerModel> players,
      IEnumerable<GameModel> games,
      long id,
      string name)
    {
      _players = new HashSet<PlayerModel>(players);
      _games = new HashSet<GameModel>(games);
      Id = id;
      Name = name;
    }
  }
}
