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

            foreach (var playerItem in player.Inventory.Items)
            {
                await _graphClient.Cypher
                    .Match("(inventory:Inventory)")
                    .Where((Inventory inventory) => player.Inventory.Id == inventory.Id)
                    .Create("(inventory)-[:PARTOF]->(item:Item $item)")
                    .WithParam("item", playerItem)
                    .ExecuteWithoutResultsAsync();
            }
                
            var startingRoad = _graphClient.Cypher
                .Match("(x:Road)")
                .Return(x => x.As<Road>())
                .ResultsAsync.Result.FirstOrDefault();

            var at = new AtRelation { IsDirectedOut = true };
            await Relate<Road, GraphRelationship>(x => x.Id == player.Id, y => y.Id == startingRoad.Id, at);
        }

        public IList<string> PlayerLocationType(Player player)
        {
            var locations = _graphClient.Cypher
                .Match("(x:Player)-[:AT]->(z)")
                .Where((Player x) => x.Id == player.Id)
                .Return(z => z.Labels())
                .ResultsAsync.Result.FirstOrDefault();

            return locations.ToList();
        }

        public Location GetPlayerLocation(Player player)
        {
            var at = new AtRelation { IsDirectedOut = true };
            return RelatedTo<Location, GraphRelationship>(x => x.Id == player.Id, y => true, at).ResultsAsync.Result.FirstOrDefault();
        }

        public Player GetPlayerWithInventory(string playerId)
        {
            var player = GetById(playerId).Result;
            player.Inventory = GetInventory(player);

            var equipped = new GraphRelationship(RelationshipConstants.Equipped, isDirectedOut: true);

            player.CurrentWeapon = RelatedTo<Weapon, GraphRelationship>(x => x.Id == playerId, y => true, equipped).ResultsAsync.Result.FirstOrDefault();
            return player;

        }

        public void MovePlayer(Player player,string currentLocationId, string newLocationId)
        {
            _graphClient.Cypher
                .Match("(p)-[r1:AT]->(oldLocation),(newLocation)")
                .Where((Player p) => p.Id == player.Id)
                .AndWhere((Location oldLocation) => oldLocation.Id == currentLocationId)
                .AndWhere((Location newLocation) => newLocation.Id == newLocationId)
                .Create("(p)-[r2:AT]->(newLocation)")
                .Set("r2=r1")
                .Delete("r1")
                .ExecuteWithoutResultsAsync().Wait();
        }


        public Inventory GetInventory(Player player)
        {
            var query = _graphClient.Cypher
                        .Call(@"n10s.inference.nodesLabelled('Item',  {catNameProp: ""Label"",catLabel: ""Topic"",subCatRel: ""SUBCLASS_OF""})").Yield($"node AS allItems")
                        .Match("(x:Player)-[r:OWN]->(inventory:Inventory)-[]->(allItems)")
                        .Where((Player x) => x.Id == player.Id)
                        .With("{Id:inventory.Id, CurrentCurrency:inventory.CurrentCurrency, MaxCurrency:inventory.MaxCurrency, Items:collect(allItems)} as playerInventory")
                        .Return(playerInventory => playerInventory.As<Inventory>());

            return query.ResultsAsync.Result.FirstOrDefault();

        }



    }
}
