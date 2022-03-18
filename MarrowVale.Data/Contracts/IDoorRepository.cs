using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.PathObstacles;


namespace MarrowVale.Data.Contracts
{
    public interface IDoorRepository : IBaseRepository<Door>
    {
        bool IsDoorOpenable(Player player, Door door);
        bool IsDoorCloseable(Player player, Door door);
        bool IsDoorUnlockable(Player player, Door door);
        bool IsDoorLockable(Player player, Door door);
        void OpenDoor(Door door);
        void CloseDoor(Door door);
        void LockDoor(Door door);
        void UnlockDoor(Door door);
        void BreakDoor(Door door);

    }
}
