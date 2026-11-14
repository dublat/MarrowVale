using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class EnviornmentalInteractionService : IEnviornmentalInteractionService
    {

        private readonly IPlayerRepository _playerRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IDoorRepository _doorRepository;
        private readonly IItemRepository _itemRepository;

        public EnviornmentalInteractionService(IPlayerRepository playerRepository, ILocationRepository locationRepository, 
                                 IDoorRepository doorRepository, IItemRepository itemRepository)
        {
            _playerRepository = playerRepository;
            _locationRepository = locationRepository;
            _doorRepository = doorRepository;
            _itemRepository = itemRepository;
        }

   
        public async Task<MarrowValeMessage> OpenDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = await _doorRepository.GetById(command.DirectObjectNode.Id);

            if (door.IsOpen)
            {
                message.ErrorText = "Door is already open placeholder";
                return message;
            }
            else if (!door.IsUnlocked)
            {
                message.ErrorText = "Door is locked placeholder";
                return message;
            }
            //else if (!_doorRepository.IsDoorOpenable(player, door))
            //{
            //    message.ErrorText = "Unable to open the door";
            //    return message;
            //}

            await _doorRepository.OpenDoor(door);
            message.ResultText = "Door Open PlaceHolder";

            return message;
        }

        public async Task<MarrowValeMessage> CloseDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = await _doorRepository.GetById(command.DirectObjectNode.Id);

            if (!door.IsOpen)
            {
                message.ErrorText = "Door is already closed placeholder";
                return message;
            }
            else if (!_doorRepository.IsDoorCloseable(player, door))
            {
                message.ErrorText = "Unable to close the door";
                return message;
            }

            await _doorRepository.CloseDoor(door);
            message.ResultText = "Door Close PlaceHolder";
            return message;
        }


        public async Task<MarrowValeMessage> UnlockDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = await _doorRepository.GetById(command.DirectObjectNode.Id);

            if (door.IsOpen)
            {
                message.ErrorText = "Can not unlock open door placeholder";
                return message;
            }
            else if (door.IsUnlocked)
            {
                message.ErrorText = "Door is already unlocked placeholder";
                return message;
            }
            else if (!await _doorRepository.HasKey(player, door))
            {
                message.ErrorText = "Unable to unlock the door";
                return message;
            }

            door.IsUnlocked = true;
            await _doorRepository.Update(door);
            message.ResultText = "Door unlocked PlaceHolder";

            return message;
        }

        public async Task<MarrowValeMessage> LockDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = await _doorRepository.GetById(command.DirectObjectNode.Id);

            if (door.IsOpen)
            {
                message.ErrorText = "Can not lock open door placeholder";
                return message;
            }
            else if (!door.IsUnlocked)
            {
                message.ErrorText = "Door is already locked placeholder";
                return message;
            }
            else if (!await _doorRepository.HasKey(player, door))
            {
                message.ErrorText = "Unable to lock the door";
                return message;
            }

            door.IsUnlocked = false;
            await _doorRepository.Update(door);
            message.ResultText = "Door locked PlaceHolder";

            return message;
        }

        public MarrowValeMessage BreakDoor(Player player, Command command)
        {
            throw new NotImplementedException();
        }


        public async Task<MarrowValeMessage> PickUpItem(Command command, Player player)
        {
            var message = new MarrowValeMessage();

            var item = await _itemRepository.GetChildrenById(command.DirectObjectNode.Id);
            var currentLocation = await _playerRepository.GetPlayerLocation(player);
            if (await _locationRepository.IsItemAtLocation(currentLocation, item))
            {
                await _itemRepository.TransferItem(player, item);
                message.ResultText = "Item aquired Placeholder";
            }
            else
            {
                message.ErrorText = "Error attempting to pick up item";
            }
            return message;
        }

        public async Task<MarrowValeMessage> DropItem(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            if (command.DirectObjectNode == null)
            {
                message.ErrorText = "Unable to drop the item";
                return message;
            }

            var item = await _itemRepository.GetChildrenById(command.DirectObjectNode.Id);
            var currentLocation = await _playerRepository.GetPlayerLocation(player);
            var isItemOwnedByPlayer = player.Inventory.Items.Any(x => x.Id == item.Id);
            if (isItemOwnedByPlayer)
            {
                await _itemRepository.TransferItem(currentLocation, item);
                message.ResultText = "Item dropped Placeholder";
            }
            else
            {
                message.ErrorText = "Unable to drop the item";
            }
            return message;
        }



        public MarrowValeMessage AttackObject(Command command, Player player)
        {
            throw new NotImplementedException();
        }


    }
}
