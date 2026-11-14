using MarrowVale.Business.Entities.Entities;

namespace MarrowVale.Business.Contracts
{
    public interface IGameSetupService
    {
        System.Threading.Tasks.Task<Player> Setup();
    }
}
