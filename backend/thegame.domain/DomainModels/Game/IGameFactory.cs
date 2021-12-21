using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Game
{
  public interface IGameFactory
  {
    Result<GameModel> CreateNewGame(string name);
  }
}
