using System;
using System.Collections.Generic;
using thegame.domain.DomainModels.Common;
using thegame.domain.DomainModels.Services;

namespace thegame.domain.DomainModels
{
    public class Game : BaseModel
    {
        public class GameFactory : IGameFactory
        {
            public GameFactory()
            {
                // TODO add dependencies
            }

            public Result<Game> CreateNewGame(string name)
            {
                var newGame = new Game
                {
                    IsActive = true,
                    Name = string.IsNullOrWhiteSpace(name) ?
                        DateTimeOffset.UtcNow.ToString("o") :
                        name
                };

                // TODO track new entity

                return Result.Success(newGame);
            }
        }

        protected Team _team;
        protected HashSet<LicensePlate> _licensePlate;

        public long Id { get; }
        public string Name { get; protected set; }
        public bool IsActive { get; protected set; }
        public DateTimeOffset? EndedOn { get; protected set; }

        public Result<Game> EndGame(DateTimeOffset endedOn)
        {
            if (!IsActive)
            {
                return Result.Error<Game>("inactive_game");
            }

            if (endedOn < CreatedOn)
            {
                return Result.Error<Game>("invalid_ended_on_date");
            }

            IsActive = false;
            EndedOn = endedOn;

            return Result.Success(this);
        }
    }
}