using thegame.domain.DomainModels.Common;

namespace thegame.domain.DomainModels.Services
{
    public interface IGameFactory
    {
        Result<Game> CreateNewGame(string name);
    }
}