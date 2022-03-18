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
            var owns = new GraphRelationship("OWNS");

            AliasManager.CreateAlias(player, item);
            AliasManager.CreateAlias(at, owns);


            await DeleteRelationshipById(item, at);
            await DeleteRelationshipById(item, owns);
            var partOf = new GraphRelationship(RelationshipConstants.PartOf, isDirectedOut: false);
            await Relate<Inventory, GraphRelationship>(y => y.Id == item.Id, x => x.Id == player.Inventory.Id, partOf);


            AliasManager.DisposeAlias(player, item);
            AliasManager.DisposeAlias(at, owns);
        }

        public async Task TransferItem(Npc npc, Item item)
        {
            AliasManager.CreateAlias(npc, item);

            var at = new AtRelation();
            AliasManager.CreateAlias(at);
            await DeleteRelationship(x => x.Id == item.Id, at);

            var owns = new GraphRelationship("OWNS", isDirectedOut: true);
            AliasManager.CreateAlias(owns);
            await DeleteRelationship(x => x.Id == item.Id, owns);

            var partOf = new GraphRelationship(RelationshipConstants.PartOf, isDirectedOut: false);
            AliasManager.CreateAlias(partOf);
            await Relate<Inventory, GraphRelationship>(y => y.Id == item.Id, x => x.Id == npc.Inventory.Id, partOf);

            AliasManager.DisposeAlias(npc, item);
            AliasManager.DisposeAlias(at, owns, partOf);
        }

        public async Task TransferItem(Location location, Item item)
        {
            var at = new AtRelation { IsDirectedOut = false };

            AliasManager.CreateAlias(location, item);
            AliasManager.CreateAlias(at);

            await DeleteRelationship(x => x.Id == item.Id, at);
            await Relate<Inventory, GraphRelationship>(y => y.Id == item.Id, x => x.Id == location.Id, at);

            AliasManager.DisposeAlias(location, item);
            AliasManager.DisposeAlias(at);
        }

    }

}
