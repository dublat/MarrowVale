using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.PathObstacles;
using System.Threading.Tasks;

namespace MarrowVale.Data.Contracts
{
    public interface IDoorRepository : IBaseRepository<Door>
    {
        bool IsDoorOpenable(Player player, Door door);
        bool IsDoorCloseable(Player player, Door door);
        bool HasKey(Player player, Door door);
        Task OpenDoor(Door door);
        Task CloseDoor(Door door);
        void BreakDoor(Door door);

    }
}
