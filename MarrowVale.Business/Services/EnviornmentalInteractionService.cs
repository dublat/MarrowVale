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
        private readonly INpcRepository _npcRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly IPromptService _promptService;
        private readonly IAiService _aiService;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IDoorRepository _doorRepository;
        private readonly IItemRepository _itemRepository;

        public EnviornmentalInteractionService(IPlayerRepository playerRepository, INpcRepository npcRepository, ILocationRepository locationRepository, 
                                 IRoomRepository roomRepository, IPromptService promptService,IAiService aiService, 
                                 IBuildingRepository buildingRepository, IDoorRepository doorRepository, IItemRepository itemRepository)
        {
            _playerRepository = playerRepository;
            _npcRepository = npcRepository;
            _locationRepository = locationRepository;
            _roomRepository = roomRepository;
            _promptService = promptService;
            _aiService = aiService;
            _buildingRepository = buildingRepository;
            _doorRepository = doorRepository;
            _itemRepository = itemRepository;
        }

   
        public MarrowValeMessage OpenDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = _doorRepository.GetById(command.DirectObjectNode.Id).Result;
            if (_doorRepository.IsDoorOpenable(player, door))
            {
                _doorRepository.OpenDoor(door);
                message.ResultText = "Door Open PlaceHolder";
            }
            else
            {
                message.ErrorText = "Unable to open the door";
            }

            return message;
        }

        public MarrowValeMessage CloseDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = _doorRepository.GetById(command.DirectObjectNode.Id).Result;
            if (_doorRepository.IsDoorCloseable(player, door))
            {
                _doorRepository.OpenDoor(door);
                message.ResultText = "Door Close PlaceHolder";
            }
            else
            {
                message.ErrorText = "Unable to close the door";
            }

            return message;
        }

        public MarrowValeMessage BreakDoor(Player player, Command command)
        {
            throw new NotImplementedException();
        }

        public MarrowValeMessage UnlockDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = _doorRepository.GetById(command.DirectObjectNode.Id).Result;
            if (_doorRepository.IsDoorUnlockable(player, door))
            {
                _doorRepository.LockDoor(door);
                message.ResultText = "Door unlocked PlaceHolder";
            }
            else
            {
                message.ErrorText = "Unable to unlock the door";
            }

            return message;
        }

        public MarrowValeMessage LockDoor(Player player, Command command)
        {
            var message = new MarrowValeMessage();
            var door = _doorRepository.GetById(command.DirectObjectNode.Id).Result;
            if (_doorRepository.IsDoorLockable(player, door))
            {
                _doorRepository.UnlockDoor(door);
                message.ResultText = "Door locked PlaceHolder";
            }
            else
            {
                message.ErrorText = "Unable to lock the door";
            }

            return message;
        }

        public MarrowValeMessage PickUpItem(Command command, Player player)
        {
            var message = new MarrowValeMessage();

            var item = _itemRepository.GetChildrenById(command.DirectObjectNode.Id).Result;
            var currentLocation = _playerRepository.GetPlayerLocation(player);
            if (_locationRepository.IsItemAtLocation(currentLocation, item))
            {
                _itemRepository.TransferItem(player, item);
                message.ResultText = "Item aquired Placeholder";
            }
            else
            {
                message.ErrorText = "Error attempting to pick up item";
            }
            return message;
        }

        public MarrowValeMessage DropItem(Command command, Player player)
        {
            var message = new MarrowValeMessage();

            var item = _itemRepository.GetById(command.DirectObjectNode.Id).Result;
            var currentLocation = _playerRepository.GetPlayerLocation(player);
            var isItemOwnedByPlayer = player.Inventory.Items.Any(x => x.Id == item.Id);
            if (isItemOwnedByPlayer)
            {
                _itemRepository.TransferItem(currentLocation, item);
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
