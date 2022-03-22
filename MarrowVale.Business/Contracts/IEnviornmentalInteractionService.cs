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
        public MarrowValeMessage PickUpItem(Command command, Player player);
        public MarrowValeMessage DropItem(Command command, Player player);
        public MarrowValeMessage AttackObject(Command command, Player player);
        public MarrowValeMessage OpenDoor(Player player, Command command);
        public MarrowValeMessage CloseDoor(Player player, Command command);
        public MarrowValeMessage BreakDoor(Player player, Command command);
        public MarrowValeMessage UnlockDoor(Player player, Command command);
        public MarrowValeMessage LockDoor(Player player, Command command);


    }
}
