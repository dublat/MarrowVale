using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.PathObstacles;
using MarrowVale.Data.Contracts;
using Neo4jClient;

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
            throw new System.NotImplementedException();
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
