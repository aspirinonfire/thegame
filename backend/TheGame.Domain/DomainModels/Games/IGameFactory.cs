using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Games
{
  public interface IGameFactory
  {
    Result<Game> CreateNewGame(string name);
  }
}