using System.Threading.Tasks;
using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Data.Contracts;
using System;
using System.Linq;
using System.Text;

namespace MarrowVale.Business.Services
{
    public class NavigationService : INavigationService
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

        public NavigationService(IPlayerRepository playerRepository, INpcRepository npcRepository, ILocationRepository locationRepository, 
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


        public async Task<bool> IsPathValid(Location playerLocation, Location destination)
        {
            return playerLocation != null && destination != null && await _locationRepository.IsPathConnected(playerLocation, destination);
        }


        public async Task<MarrowValeMessage> Enter(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var playerLocation = await _playerRepository.GetPlayerLocation(player);
            var roomDestination = await EnterDestination(command);
            if (await IsPathValid(playerLocation, roomDestination))
            {
                await _playerRepository.MovePlayer(player, playerLocation.Id, roomDestination.Id);
                var roomDescription = await GenerateRoomFlavorText(roomDestination);
                message.ResultText = roomDescription;
            }
            else
            {
                message.ErrorText = $"Unable to navigate the following path to {roomDestination?.Name ?? command?.DirectObjectNode?.Name}";
            }

            return message;
        }

        public async Task<MarrowValeMessage> Exit(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var playerLocation = await _playerRepository.GetPlayerLocation(player);
            var destination = await exitDestination(command);
            if (await IsPathValid(playerLocation, destination))
            {
                await _playerRepository.MovePlayer(player, playerLocation.Id, destination.Id);
                message = await CurrentLocationDescription(player);
            }
            else
            {
                message.ErrorText = $"Unable to exit location {destination?.Name ?? command?.DirectObjectNode?.Name}";
            }

            return message;
        }

        public async Task<MarrowValeMessage> TraversePath(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var playerLocation = await _playerRepository.GetPlayerLocation(player);
            var endLocation = await pathDestination(command);

            if (await IsPathValid(playerLocation, endLocation))
            {
                await _playerRepository.MovePlayer(player, playerLocation.Id, endLocation.Id);
                message.ResultText = await generateRoadFlavorText(endLocation);
            }
            else
            {
                message.ErrorText = $"Unable to traverse road {endLocation?.Name ?? command?.DirectObjectNode?.Name}";
            }

            return message;
        }


        public string ClimbDown(Player player, Location location)
        {
            throw new NotImplementedException();
        }

        public string ClimbUp(Player player, Location location)
        {
            throw new NotImplementedException();
        }

        public string EnterBuilding(Player player, Building building)
        {
            throw new NotImplementedException();
        }

        public string ExitBuilding(Player player)
        {
            throw new NotImplementedException();
        }

        public string FastTravel(Player player, Location location)
        {
            throw new NotImplementedException();
        }



        private async Task<Room> EnterDestination(Command command)
        {
            var destinationId = command.DirectObjectNode.Id;
            var buildingEntity = "Building";

            if (command.DirectObjectNode.Labels.Contains(buildingEntity))
                return await _locationRepository.GetBuildingEntrance(destinationId);
            else
                return await _roomRepository.Single(x => x.Id == destinationId);
        }

        private async Task<Location> exitDestination(Command command)
        {
            var destinationId = command.DirectObjectNode.Id;
            var buildingEntity = "Building";

            if (command.DirectObjectNode.Labels.Contains(buildingEntity))
            return await _locationRepository.GetBuildingExit(command.DirectObjectNode.Id);
            else
                return await _locationRepository.Single(x => x.Id == destinationId);
        }


        private async Task<Location> pathDestination(Command command)
        {
            var destinationId = command.DirectObjectNode.Id;
            return await _locationRepository.Single(x => x.Id == destinationId);
        }


        public async Task<MarrowValeMessage> CurrentLocationDescription(Player player)
        {
            var message = new MarrowValeMessage();

            var currentLocationType = await _playerRepository.PlayerLocationType(player);
            var currentLocation = await _playerRepository.GetPlayerLocation(player);
            if (currentLocationType.Contains("Road"))
            {
                message.ResultText = await generateRoadFlavorText(currentLocation);
            }
            else if (currentLocationType.Contains("Building"))
            {
                message.ResultText = await generateBuildingFlavorText(currentLocation);
            }
            else if (currentLocationType.Contains("Room"))
            {

                message.ResultText = await GenerateRoomFlavorText(currentLocation);
            }
            else
            {
                message.ErrorText = "Error";
            }
            return message;
        }



        private async Task<string> GenerateRoomFlavorText(Location room)
        {
            var input = room.DescriptionPromptInput();
            room.Description = input.ToString();
            WorldContextService.ContextNodes.Add(room);

            var prompt = _promptService.GenerateSummaryDescription(input.ToString(), SummaryTypeEnum.Room);
            var roomOutput = await _aiService.Complete(prompt);
            var peopleInsideRoom = await _locationRepository.GetNpcsAtLocation(room);

            var npcInput = new StringBuilder($"Location: {roomOutput}\n");
            foreach (var npc in peopleInsideRoom)
            {
                if (npc.Visible)
                {
                    npc.Description = npc.DescriptionPromptInput();
                    WorldContextService.ContextNodes.Add(npc);
                    npcInput.Append(npc.Description);
                }
            }

            var npcPrompt = _promptService.GenerateSummaryDescription(npcInput.ToString(), SummaryTypeEnum.Npcs);
            var npcOutput = await _aiService.Complete(npcPrompt);

            var connectingRooms = await _locationRepository.GetConnectingRooms(room);
            var connectingRoomsInput = new StringBuilder();
            foreach (var connectingRoom in connectingRooms)
            {
                connectingRoom.Description = connectingRoom.ShortDescriptionPrompt().ToString();
                WorldContextService.ContextNodes.Add(connectingRoom);
                connectingRoomsInput.Append(connectingRoom.Description);
            }
            var connectingRoomPrompt = _promptService.GenerateSummaryDescription(connectingRoomsInput.ToString(), SummaryTypeEnum.ConnectingRooms);
            var connectingRoomOutput = await _aiService.Complete(connectingRoomPrompt);


            var items = await _itemRepository.GetItemsAtLocation(room.Id);
            var itemsString = new StringBuilder("Items in room:");
            foreach (var item in items)
            {
                itemsString.Append($"Name: {item.Name}");
            }

            return roomOutput + npcOutput + connectingRoomOutput + itemsString.ToString();
        }

        private async Task<string> generateRoadFlavorText(Location road)
        {
            var nearbyBuildings = await _locationRepository.GetNearbyBuildings(road);
            var nearbyRoads = await _locationRepository.GetNearbyRoads(road);
            var input = new StringBuilder();
            foreach (var building in nearbyBuildings)
            {
                WorldContextService.ContextNodes.Add(building);
                input.Append(building.ShortExteriorDescription());
            }
            foreach (var localRoad in nearbyRoads)
            {
                WorldContextService.ContextNodes.Add(localRoad);
                input.Append(localRoad.ShortExteriorDescription());
            }
            var prompt = _promptService.GenerateSummaryDescription(input.ToString(), SummaryTypeEnum.Road);

            return await _aiService.Complete(prompt);
        }

        private async Task<string> generateBuildingFlavorText(Location location)
        {
            var building = await _buildingRepository.Single(x => x.Id == location.Id);
            var input = building.DescriptionPromptInput();
            var roomPrompt = _promptService.GenerateSummaryDescription(input.ToString(), SummaryTypeEnum.Building);
            var roomOutput = await _aiService.Complete(roomPrompt);

            var peopleInsideRoom = await _locationRepository.GetNpcsAtLocation(location);
            var npcInput = _promptService.GenerateSummaryDescription(input.ToString(), SummaryTypeEnum.Npcs);
            foreach (var npc in peopleInsideRoom)
            {
                npcInput.Input.Concat(npc.DescriptionPromptInput().ToString());
            }
            var npcPrompt = _promptService.GenerateSummaryDescription(npcInput.ToString(), SummaryTypeEnum.Npcs);
            var npcOutput = await _aiService.Complete(roomPrompt);


            return roomOutput + npcOutput;
        }


    }
}
