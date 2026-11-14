using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Contracts
{
    public interface IEnviornmentalInteractionService
    {
        public Task<MarrowValeMessage> PickUpItem(Command command, Player player);
        public Task<MarrowValeMessage> DropItem(Command command, Player player);
        public MarrowValeMessage AttackObject(Command command, Player player);
        public Task<MarrowValeMessage> OpenDoor(Player player, Command command);
        public Task<MarrowValeMessage> CloseDoor(Player player, Command command);
        public MarrowValeMessage BreakDoor(Player player, Command command);
        public Task<MarrowValeMessage> UnlockDoor(Player player, Command command);
        public Task<MarrowValeMessage> LockDoor(Player player, Command command);


    }
}
