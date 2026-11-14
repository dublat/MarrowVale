using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.Graph;
using MarrowVale.Business.Entities.Entities.Relationships;
using MarrowVale.Common.Constants;
using MarrowVale.Data.Contracts;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarrowVale.Data.Repositories
{
    public class NpcRepository : BaseRepository<Npc>, INpcRepository
    {
        public NpcRepository(IGraphClient graphClient) : base(graphClient)
        {

        }

        public async Task<Npc> GetNpcByName(string firstName)
        {
            return (await Single(x => x.Name == firstName));
        }

        public async Task<bool> IsPlayerNearby(Player player, Npc npc)
        {
            if (npc == null || player == null)
                return false;

            var inside = new GraphRelationship(RelationshipConstants.Inside);
            inside.IsDirectedOut = true;
            var at = new AtRelation { IsDirectedOut = false };

            return (await RelatedToAndFrom<Room, Player, GraphRelationship>(x => x.Id == npc.Id, z => true, y => y.Id == player.Id, inside, at).ResultsAsync).Any();

        }

        public async Task<ConversationNode> MemoryofPlayer(Npc npc, Player player)
        {
            return (await _graphClient.Cypher
                .Match("(x:Character)-[:REMEMBERS|MEMORY]-(z:Conversation)-[:MENTIONS|MENTIONED]-(y:Player)")
                .Where((Npc x) => x.Id == npc.Id)
                .AndWhere((Player y) => y.Id == player.Id)
                .Return(z => z.As<ConversationNode>())
                .OrderByDescending("coalesce(z.CreatedAt, 0)")
                .Limit(1).ResultsAsync).FirstOrDefault();
        }

        public async Task<IEnumerable<BaseRelationship>> AllMemories(Npc npc)
        {
            return (await _graphClient.Cypher
                        .Match("(x:Character)-[r:REMEMBERS|MEMORY]-(z)")
                        .Where((Npc x) => x.Id == npc.Id)
                        .With("{Relation: type(r), Node: z} as objectNode")
                        .Return(objectNode => objectNode.CollectAs<BaseRelationship>()).ResultsAsync).FirstOrDefault();
        }

        public async Task<GraphNode> AllKnowledge(Npc npc)
        {
            var knowledge = (await _graphClient.Cypher
                        .Match("(x:Character)-[r]->(item)")
                        .Where((Npc x) => x.Id == npc.Id)
                        .AndWhere("NOT item:Conversation")
                        .With("{Id: x.Id, Name: x.Name, Relations: collect({Relation: type(r), Node: item})} as characterNode")
                        .Return(characterNode => characterNode.As<GraphNode>()).ResultsAsync).FirstOrDefault();

            foreach (var k in knowledge?.Relations ?? Enumerable.Empty<BaseRelationship>())
            {
                var metaKnowledge = await _graphClient.Cypher
                        .Match("(item)-[r:PARTOF]->(subItem)")
                        .Where((GraphNode item) => item.Id == k.Node.Id)
                        .With("{Relation: type(r), Node: subItem} as test")
                        .Return(test => test.As<BaseRelationship>()).ResultsAsync;
                k.Node.Relations = (k.Node.Relations ?? Enumerable.Empty<BaseRelationship>()).Concat(metaKnowledge);
            }


            return knowledge;
        }

        public async Task<IEnumerable<Item>> MerchantInventory(Npc npc)
        {
            return (await _graphClient.Cypher
                        .Match("(x:Character)-[r:SELLS]->(z)-[]->(item:Item)")
                        .Where((Npc x) => x.Id == npc.Id)
                        .Return(item => item.As<Item>()).ResultsAsync);
        }

        public async Task<Item> BuyItem(Npc npc, Player player, Item item, int price, int quantity = 1)
        {
            return (await _graphClient.Cypher
                .Match("(seller:Character)-[r1:SELLS]->(sellerInventory:Inventory)-[r2:PARTOF]->(tradedItem:Item),(p:Player)-[r3:OWNS]->(playerInventory:Inventory)")
                .Where((Player p) => p.Id == player.Id)
                .AndWhere((Npc seller) => seller.Id == npc.Id)
                .AndWhere((Item tradedItem) => tradedItem.Id == item.Id)
                .Set($"playerInventory.CurrentCurrency = playerInventory.CurrentCurrency - {price}")
                .Set($"sellerInventory.CurrentCurrency = sellerInventory.CurrentCurrency + {price}")
                .Create("(playerInventory)-[:PARTOF]->(tradedItem)")
                .Delete("r2")
                .Return(tradedItem => tradedItem.As<Item>()).ResultsAsync).FirstOrDefault();
        }

        public async Task<Item> SellItem(Npc npc, Player player, Item item, int price, int quantity = 1)
        {
            var own = RelationshipConstants.Own;
            var partOf = RelationshipConstants.PartOf;
            var sells = RelationshipConstants.Sells;

            return (await _graphClient.Cypher
                .Match($"(p:Player)-[r1:{own}]->(playerInventory:Inventory)-[r2:{partOf}]->(tradedItem:Item),(merchant:Character)-[r3:{sells}]->(merchantInventory:Inventory)")
                .Where((Player p) => p.Id == player.Id)
                .AndWhere((Npc merchant) => merchant.Id == npc.Id)
                .AndWhere((Item tradedItem) => tradedItem.Id == item.Id)
                .Set($"playerInventory.CurrentCurrency = playerInventory.CurrentCurrency + {price}")
                .Set($"merchantInventory.CurrentCurrency = merchantInventory.CurrentCurrency - {price}")
                .Create("(merchantInventory)-[:PARTOF]->(tradedItem)")
                .Delete("r2")
                .Return(tradedItem => tradedItem.As<Item>()).ResultsAsync).FirstOrDefault();
        }

        public async Task CreateMemory(Npc c, Player player, string s)
        {
            var conversation = new ConversationNode { Id = Guid.NewGuid().ToString(), Summary = s };

            var created = await _graphClient.Cypher
                .Match("(npc:Character)", "(p:Player)")
                .Where((Npc npc) => npc.Id == c.Id)
                .AndWhere((Player p) => p.Id == player.Id)
                .Create("(npc)-[:REMEMBERS]->(con:Conversation $memory)-[:MENTIONS]->(p)")
                .WithParam("memory", conversation)
                .Set("con.CreatedAt = timestamp()")
                .Return(con => con.As<ConversationNode>())
                .ResultsAsync;
            if (!created.Any())
                throw new InvalidOperationException("Memory creation requires an existing NPC and player.");

        }

        public async Task SetCombatEquipment(Npc npc)
        {
            var equipped = new GraphRelationship(RelationshipConstants.Equipped, isDirectedOut: true);

            npc.Armor = (await RelatedTo<Armor, GraphRelationship>(x => x.Id == npc.Id, y => true, equipped).ResultsAsync).FirstOrDefault();
            npc.Weapon = (await RelatedTo<Weapon, GraphRelationship>(x => x.Id == npc.Id, y => true, equipped).ResultsAsync).FirstOrDefault();
        }

        public async Task SaveCombatEquipment(Npc npc)
        {
            var equipped = new GraphRelationship(RelationshipConstants.Equipped, isDirectedOut: true);
            if (npc.Armor != null)
                await AddAndRelate(x => x.Id == npc.Id, npc.Armor, equipped);
            if (npc.Weapon != null)
                await AddAndRelate(x => x.Id == npc.Id, npc.Weapon, equipped);
        }

    }
}
