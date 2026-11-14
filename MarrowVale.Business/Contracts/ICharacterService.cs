using System.Threading.Tasks;
using MarrowVale.Business.Entities.Dtos;
using MarrowVale.Business.Entities.Entities;

namespace MarrowVale.Business.Contracts
{
    public interface ICharacterService
    {
        Task<Player> NewCharacter();
        Task<Player> LoadCharacter();
        Task<Inventory> GetInventory(Player player);
    }
}
