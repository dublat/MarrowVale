using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Data.Contracts
{
    public interface INpcRepository : IBaseRepository<Npc>
    {
        Task<Npc> GetNpcByName(string firstName);
        Task<bool> IsPlayerNearby(Player player, Npc npc);
        Task<ConversationNode> MemoryofPlayer(Npc npc, Player player);
        Task CreateMemory(Npc c, Player player, string s);
        Task<IEnumerable<BaseRelationship>> AllMemories(Npc npc);
        Task<IEnumerable<Item>> MerchantInventory(Npc npc);
        Task<Item> BuyItem(Npc npc, Player player, Item item, int price, int quantity = 1);
        Task<Item> SellItem(Npc npc, Player player, Item item, int price, int quantity = 1);
        Task<GraphNode> AllKnowledge(Npc npc);
        Task SetCombatEquipment(Npc npc);
        Task SaveCombatEquipment(Npc npc);


    }
}
