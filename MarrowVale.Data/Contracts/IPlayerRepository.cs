using MarrowVale.Business.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarrowVale.Data.Contracts
{
    public interface IPlayerRepository : IBaseRepository<Player>
    {
        Task CreatePlayer(Player player);
        Task MovePlayer(Player player, string currentLocationId, string newLocationId);
        Task<IList<string>> PlayerLocationType(Player player);
        Task<Location> GetPlayerLocation(Player player);
        Task<Player> GetPlayerWithInventory(string playerId);
        Task<Inventory> GetInventory(Player player);
    }
}
