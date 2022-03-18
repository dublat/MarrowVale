using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.Relationships;
using MarrowVale.Common.Constants;
using MarrowVale.Data.Contracts;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Data.Repositories
{
    public class ItemRepository : BaseRepository<Item>, IItemRepository
    {
        public ItemRepository(IGraphClient graphClient) : base(graphClient)
        {
        }

        public IEnumerable<Item> GetItemsAtLocation(string locationId)
        {
            var at = new AtRelation { IsDirectedOut = false };
            var preQuery = FromChildCategory();
            return RelatedFrom<Location, GraphRelationship>(x => true, y => y.Id == locationId, at, preQuery: preQuery).ResultsAsync.Result;
        }

        public async Task TransferItem(Player player, Item item)
        {
            var at = new AtRelation();
            await DeleteRelationship(x => x.Id == item.Id, at);

            var owns = new GraphRelationship("OWNS");
            await DeleteRelationship(x => x.Id == item.Id, owns);

            var partOf = new GraphRelationship(RelationshipConstants.PartOf, isDirectedOut: false);

            await Relate<Inventory, GraphRelationship>(y => y.Id == item.Id, x => x.Id == player.Inventory.Id, partOf);
        }

        public async Task TransferItem(Npc npc, Item item)
        {
            var at = new AtRelation();
            await DeleteRelationship(x => x.Id == item.Id, at);

            var owns = new GraphRelationship("OWNS", isDirectedOut: true);
            await DeleteRelationship(x => x.Id == item.Id, owns);

            var partOf = new GraphRelationship(RelationshipConstants.PartOf, isDirectedOut: false);

            await Relate<Inventory, GraphRelationship>(y => y.Id == item.Id, x => x.Id == npc.Inventory.Id, partOf);
        }

        public async Task TransferItem(Location location, Item item)
        {
            var at = new AtRelation { IsDirectedOut = false };
            await DeleteRelationship(x => x.Id == item.Id, at);

            await Relate<Inventory, GraphRelationship>(y => y.Id == item.Id, x => x.Id == location.Id, at);
        }

    }

}
