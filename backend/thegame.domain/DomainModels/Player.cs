using thegame.domain.DomainModels.Common;

namespace thegame.domain.DomainModels
{
    public class Player : BaseModel
    {
        protected Team _team;

        public long UserId { get; }
        public string Name { get; }

    }
}