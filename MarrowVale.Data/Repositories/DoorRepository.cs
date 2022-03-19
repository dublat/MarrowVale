using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.PathObstacles;
using MarrowVale.Common.Constants;
using MarrowVale.Data.Contracts;
using Neo4jClient;
using System.Linq;

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

        public void CloseDoor(Door door)
        {
            throw new System.NotImplementedException();
        }

        public bool IsDoorCloseable(Player player, Door door)
        {
            throw new System.NotImplementedException();
        }

        public bool IsDoorLockable(Player player, Door door)
        {
            var own = RelationshipConstants.Own;
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

        public bool IsDoorUnlockable(Player player, Door door)
        {
            throw new System.NotImplementedException();
        }

        public void LockDoor(Door door)
        {
            throw new System.NotImplementedException();
        }

        public void OpenDoor(Door door)
        {
            throw new System.NotImplementedException();
        }

        public void UnlockDoor(Door door)
        {
            throw new System.NotImplementedException();
        }
    }

}
