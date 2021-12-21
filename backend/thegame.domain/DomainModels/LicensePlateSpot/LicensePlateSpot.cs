using TheGame.Domain.DomainModels.Game;
using TheGame.Domain.DomainModels.LicensePlate;
using TheGame.Domain.DomainModels.Player;

namespace TheGame.Domain.DomainModels.LicensePlateSpot
{
    public class LicensePlateSpot
    {
        protected GameModel _game { get; }
        protected LicensePlateModel _licensePlate { get; }
        protected PlayerModel _spottedBy { get; }
    }
}
