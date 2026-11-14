using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.Relationships;
using MarrowVale.Common.Constants;
using MarrowVale.Common.Contracts;
using MarrowVale.Data.Contracts;
using Microsoft.Extensions.Logging;
using Neo4jClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarrowVale.Data.Repositories
{
    public class PlayerRepository : BaseRepository<Player>, IPlayerRepository
    {
        public PlayerRepository(IGraphClient graphClient) : base(graphClient)
        {

        }


        public async Task CreatePlayer(Player player)
        {
            await Add(player);
            await AddAndRelate(x => x.Id == player.Id, player.Inventory, new GraphRelationship(RelationshipConstants.Own, isDirectedOut: true));
            await AddAndRelate(x => x.Id == player.Id, player.CurrentWeapon, new GraphRelationship(RelationshipConstants.Equipped, isDirectedOut: true));
            await AddAndRelate(x => x.Id == player.Id, player.CurrentArmor, new GraphRelationship(RelationshipConstants.Equipped, isDirectedOut: true));

            var partOf = RelationshipConstants.PartOf;
            foreach (var playerItem in player.Inventory.Items)
            {
                await _graphClient.Cypher
                    .Match("(inventory:Inventory)")
                    .Where((Inventory inventory) => player.Inventory.Id == inventory.Id)
                    .Create($"(inventory)-[:{partOf}]->(item:Item $item)")
                    .WithParam("item", playerItem)
                    .ExecuteWithoutResultsAsync();
            }
                
            var startingRoad = (await _graphClient.Cypher
                .Match("(x:Road)")
                .Return(x => x.As<Road>())
                .ResultsAsync).FirstOrDefault();

            var at = new AtRelation { IsDirectedOut = true };
            await Relate<Road, GraphRelationship>(x => x.Id == player.Id, y => y.Id == startingRoad.Id, at);
        }

        public async Task<IList<string>> PlayerLocationType(Player player)
        {
            var at = RelationshipConstants.At;
            var locations = (await _graphClient.Cypher
                .Match($"(x:Player)-[:{at}]->(z)")
                .Where((Player x) => x.Id == player.Id)
                .Return(z => z.Labels())
                .ResultsAsync).FirstOrDefault();

            return locations.ToList();
        }

        public async Task<Location> GetPlayerLocation(Player player)
        {
            var at = new AtRelation { IsDirectedOut = true };
            return (await RelatedTo<Location, GraphRelationship>(x => x.Id == player.Id, y => true, at).ResultsAsync).FirstOrDefault();
        }

        public async Task<Player> GetPlayerWithInventory(string playerId)
        {
            var player = (await GetById(playerId));
            player.Inventory = await GetInventory(player);

            var equipped = new GraphRelationship(RelationshipConstants.Equipped, isDirectedOut: true);
            player.CurrentWeapon = (await RelatedTo<Weapon, GraphRelationship>(x => x.Id == playerId, y => true, equipped).ResultsAsync).FirstOrDefault();
            return player;

        }

        public async Task MovePlayer(Player player,string currentLocationId, string newLocationId)
        {
            var at = RelationshipConstants.At;
            await _graphClient.Cypher
                .Match($"(p)-[r1:{at}]->(oldLocation),(newLocation)")
                .Where((Player p) => p.Id == player.Id)
                .AndWhere((Location oldLocation) => oldLocation.Id == currentLocationId)
                .AndWhere((Location newLocation) => newLocation.Id == newLocationId)
                .Create($"(p)-[r2:{at}]->(newLocation)")
                .Set("r2=r1")
                .Delete("r1")
                .ExecuteWithoutResultsAsync();
        }


        public async Task<Inventory> GetInventory(Player player)
        {
            var own = RelationshipConstants.Own;
            var query = _graphClient.Cypher
                        .Call(@"n10s.inference.nodesLabelled('Item',  {catNameProp: ""Label"",catLabel: ""Topic"",subCatRel: ""SUBCLASS_OF""})").Yield($"node AS allItems")
                        .Match($"(x:Player)-[r:{own}]->(inventory:Inventory)-[]->(allItems)")
                        .Where((Player x) => x.Id == player.Id)
                        .With("{Id:inventory.Id, CurrentCurrency:inventory.CurrentCurrency, MaxCurrency:inventory.MaxCurrency, Items:collect(allItems)} as playerInventory")
                        .Return(playerInventory => playerInventory.As<Inventory>());

            return (await query.ResultsAsync).FirstOrDefault();
        }



    }
}
