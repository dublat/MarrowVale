using System.Threading.Tasks;
using MarrowVale.Business.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace MarrowVale.Business.Contracts
{
    public interface ICombatService
    {
        Task<string> Attack(Player player, Npc npc);
    }
}
