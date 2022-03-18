using System.Collections.Generic;
using System.Threading.Tasks;
using MarrowVale.Business.Entities.Entities;

namespace MarrowVale.Data.Contracts
{
    public interface IItemRepository : IBaseRepository<Item>
    {
        public IEnumerable<Item> GetItemsAtLocation(string locationId);
        public Task TransferItem(Player player, Item item);
        public Task TransferItem(Npc npc, Item item);
        public Task TransferItem(Location location, Item item);
    }
}
