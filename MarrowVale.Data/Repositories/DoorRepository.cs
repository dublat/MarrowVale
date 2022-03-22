using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.PathObstacles;
using MarrowVale.Business.Entities.Entities.Relationships;
using MarrowVale.Common.Constants;
using MarrowVale.Data.Contracts;
using Neo4jClient;
using System.Linq;
using System.Threading.Tasks;

namespace MarrowVale.Data.Repositories
{
    public class DoorRepository : BaseRepository<Door>, IDoorRepository
    {
        public DoorRepository(IGraphClient graphClient) : base(graphClient)
        {
        }

        public void BreakDoor(Door door)
        {
            throw new System.NotImplementedException();
        }

        public async Task CloseDoor(Door door)
        {
            var leadsTo = RelationshipConstants.LeadsTo;
            var path = RelationshipConstants.Path;

            var query = _graphClient.Cypher
                .Match($"(location1)-[:{leadsTo}]->(d:{door.EntityLabel})<-[:{leadsTo}]-(location2),(location1)-[path:{path}]-(location2)")
                .Where((Door d) => d.Id == door.Id)
                .Set("path.IsObstructed = true");

            await query.ExecuteWithoutResultsAsync();

            door.IsOpen = false;
            await Update(door);
        }

        public async Task OpenDoor(Door door)
        {
            var leadsTo = RelationshipConstants.LeadsTo;
            var path = RelationshipConstants.Path;

            var query = _graphClient.Cypher
                .Match($"(location1)-[:{leadsTo}]->(d:{door.EntityLabel})<-[:{leadsTo}]-(location2),(location1)-[path:{path}]-(location2)")
                .Where((Door d) => d.Id == door.Id)
                .Set("path.IsObstructed = false");

            await query.ExecuteWithoutResultsAsync();

            door.IsOpen = true;
            await Update(door);
        }

        public bool IsDoorCloseable(Player player, Door door)
        {
            return true;
        }

        public bool IsDoorLockable(Player player, Door door)
        {
            var own = RelationshipConstants.Own;
            var at = RelationshipConstants.At;
            var partOf = RelationshipConstants.PartOf;
            var unlocks = RelationshipConstants.Unlocks;

            var query = _graphClient.Cypher
                .Match($"(x:Player)-[r:{own}]->(inventory:Inventory)-[:{partOf}]->(:DoorKey)-[:{unlocks}]->(d:Door), (x)-[:{at}]->()-[:LEADS_TO]->(d)")
                .Where((Player x) => x.Id == player.Id)
                .AndWhere((Door d) => d.Id == door.Id)
                .Return(x => x.As<Player>());

            return query.ResultsAsync.Result.Any();
        }

        public bool IsDoorOpenable(Player player, Door door)
        {
            throw new System.NotImplementedException();
        }

        public bool HasKey(Player player, Door door)
        {
            var own = RelationshipConstants.Own;
            var at = RelationshipConstants.At;
            var partOf = RelationshipConstants.PartOf;
            var unlocks = RelationshipConstants.Unlocks;

            var query = _graphClient.Cypher
                .Match($"(x:Player)-[r:{own}]->(inventory:Inventory)-[:{partOf}]->(:DoorKey)-[:{unlocks}]->(d:Door), (x)-[:{at}]->()-[:LEADS_TO]->(d)")
                .Where((Player x) => x.Id == player.Id)
                .AndWhere((Door d) => d.Id == door.Id)
                .Return(x => x.As<Player>());

            return query.ResultsAsync.Result.Any();
        }

    }

}
