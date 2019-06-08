﻿using MarrowVale.Business.Entities.Entities;
using System.Collections.Generic;

namespace MarrowVale.Data.Contracts
{
    public interface IPlayerRepository
    {
        IList<Player> GetPlayers();
        void AddPlayer(Player player);
        Player GetPlayer(string playerName);
    }
}