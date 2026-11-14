using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Data.Contracts;
using System;
using MarrowVale.Business.Entities.Entities.PathObstacles;
using System.Linq;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class EnviornmentalInteractionService : IEnviornmentalInteractionService
    {

        private readonly IPlayerRepository _playerRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IDoorRepository _doorRepository;
        private readonly IItemRepository _itemRepository;

        private readonly IPromptService _prompts;
        private readonly IAiService _ai;

        public EnviornmentalInteractionService(IPlayerRepository playerRepository, ILocationRepository locationRepository, 
                                 IDoorRepository doorRepository, IItemRepository itemRepository, IPromptService prompts, IAiService ai)
        {
            _prompts = prompts;
            _ai = ai;
            _playerRepository = playerRepository;
            _locationRepository = locationRepository;
            _doorRepository = doorRepository;
            _itemRepository = itemRepository;
        }

   
        public Task<MarrowValeMessage> OpenDoor(Player player, Command command) =>
            InteractWithDoor(player, command, CommandEnum.Open);

        public Task<MarrowValeMessage> CloseDoor(Player player, Command command) =>
            InteractWithDoor(player, command, CommandEnum.Close);

        public Task<MarrowValeMessage> UnlockDoor(Player player, Command command) =>
            InteractWithDoor(player, command, CommandEnum.Unlock);

        public Task<MarrowValeMessage> LockDoor(Player player, Command command) =>
            InteractWithDoor(player, command, CommandEnum.Lock);

        private async Task<MarrowValeMessage> InteractWithDoor(Player player, Command command, CommandEnum action)
        {
            var id = command?.DirectObjectNode?.Id;
            var door = string.IsNullOrWhiteSpace(id) ? null : await _doorRepository.GetById(id);
            var error = action switch
            {
                _ when door == null => "Door not found.",
                CommandEnum.Open when door.IsOpen => "Door is already open.",
                CommandEnum.Open when !door.IsUnlocked => "Door is locked.",
                CommandEnum.Close when !door.IsOpen => "Door is already closed.",
                CommandEnum.Close when !_doorRepository.IsDoorCloseable(player, door) => "Unable to close the door.",
                CommandEnum.Lock or CommandEnum.Unlock when door.IsOpen => "Close the door first.",
                CommandEnum.Lock when !door.IsUnlocked => "Door is already locked.",
                CommandEnum.Unlock when door.IsUnlocked => "Door is already unlocked.",
                _ => null
            };
            if (error != null)
                return new MarrowValeMessage { ErrorText = error };

            DoorKey key = null;
            if (action is CommandEnum.Lock or CommandEnum.Unlock)
            {
                key = await _doorRepository.GetKey(player, door);
                if (key == null)
                    return new MarrowValeMessage { ErrorText = "You need the matching key." };
            }

            var narration = await _ai.Complete(_prompts.GenerateDoorActionPrompt(action, door, key));
            if (action == CommandEnum.Open)
                await _doorRepository.OpenDoor(door);
            else if (action == CommandEnum.Close)
                await _doorRepository.CloseDoor(door);
            else
            {
                door.IsUnlocked = action == CommandEnum.Unlock;
                await _doorRepository.Update(door);
            }

            return new MarrowValeMessage
            {
                ResultText = string.IsNullOrWhiteSpace(narration) ? $"You {action.ToString().ToLowerInvariant()} the door." : narration
            };
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
