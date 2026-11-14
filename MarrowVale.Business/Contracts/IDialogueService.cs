using MarrowVale.Business.Entities.Entities;
using System.Threading.Tasks;

namespace MarrowVale.Business.Contracts
{
    public interface IDialogueService
    {
        Task Converse(Player player, Npc npc);
    }
}
