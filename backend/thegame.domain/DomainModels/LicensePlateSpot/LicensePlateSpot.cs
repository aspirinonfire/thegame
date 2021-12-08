using thegame.domain.DomainModels.Game;
using thegame.domain.DomainModels.LicensePlate;
using thegame.domain.DomainModels.Player;

namespace thegame.domain.DomainModels.LicensePlateSpot
{
    public class LicensePlateSpot
    {
        protected GameModel _game { get; }
        protected LicensePlateModel _licensePlate { get; }
        protected PlayerModel _spottedBy { get; }
    }
}
