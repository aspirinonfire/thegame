﻿using TheGame.Domain.DomainModels.Common;

namespace TheGame.Domain.DomainModels.Players
{
  public interface IPlayerFactory
  {
    DomainResult<Player> CreateNewPlayer(long userId, string name);
  }
}