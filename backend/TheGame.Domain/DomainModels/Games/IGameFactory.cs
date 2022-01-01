using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Games
{
  public interface IGameFactory
  {
    DomainResult<Game> CreateNewGame(string name);
  }
}
