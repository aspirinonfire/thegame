using thegame.domain.DomainModels.Common;

namespace thegame.domain.DomainModels.Game
{
  public interface IGameFactory
  {
    Result<GameModel> CreateNewGame(string name);
  }
}
