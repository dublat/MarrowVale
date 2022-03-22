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


        public bool IsPathValid(Location playerLocation, Location destination)
        {
            return _locationRepository.IsPathConnected(playerLocation, destination);
        }


        public MarrowValeMessage Enter(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var playerLocation = _playerRepository.GetPlayerLocation(player);
            var roomDestination = enterDestination(command);
            if (IsPathValid(playerLocation, roomDestination))
            {
                _playerRepository.MovePlayer(player, playerLocation.Id, roomDestination.Id);
                var roomDescription = generateRoomFlavorText(roomDestination);
                message.ResultText = roomDescription;
            }
            else
            {
                message.ErrorText = $"Unable to navigate the following path to {roomDestination?.Name ?? command?.DirectObjectNode?.Name}";
            }

            return message;
        }

        public MarrowValeMessage Exit(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var playerLocation = _playerRepository.GetPlayerLocation(player);
            var destination = exitDestination(command);
            if (IsPathValid(playerLocation, destination))
            {
                _playerRepository.MovePlayer(player, playerLocation.Id, destination.Id);
                message = CurrentLocationDescription(player);
            }
            else
            {
                message.ErrorText = $"Unable to exit location {destination?.Name ?? command?.DirectObjectNode?.Name}";
            }

            return message;
        }

        public MarrowValeMessage TraversePath(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var playerLocation = _playerRepository.GetPlayerLocation(player);
            var endLocation = pathDestination(command);

            if (IsPathValid(playerLocation, endLocation))
            {
                _playerRepository.MovePlayer(player, playerLocation.Id, endLocation.Id);
                message.ResultText = generateRoadFlavorText(endLocation);
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



        private Room enterDestination(Command command)
        {
            var destinationId = command.DirectObjectNode.Id;
            var buildingEntity = "Building";

            if (command.DirectObjectNode.Labels.Contains(buildingEntity))
                return _locationRepository.GetBuildingEntrance(destinationId);
            else
                return _roomRepository.Single(x => x.Id == destinationId).Result;
        }

        private Location exitDestination(Command command)
        {
            var destinationId = command.DirectObjectNode.Id;
            var buildingEntity = "Building";

            if (command.DirectObjectNode.Labels.Contains(buildingEntity))
            return _locationRepository.GetBuildingExit(command.DirectObjectNode.Id);
            else
                return _locationRepository.Single(x => x.Id == destinationId).Result;
        }


        private Location pathDestination(Command command)
        {
            var destinationId = command.DirectObjectNode.Id;
            return _locationRepository.Single(x => x.Id == destinationId).Result;
        }


        public MarrowValeMessage CurrentLocationDescription(Player player)
        {
            var message = new MarrowValeMessage();

            var currentLocationType = _playerRepository.PlayerLocationType(player);
            var currentLocation = _playerRepository.GetPlayerLocation(player);
            if (currentLocationType.Contains("Road"))
            {
                message.ResultText = generateRoadFlavorText(currentLocation);
            }
            else if (currentLocationType.Contains("Building"))
            {
                message.ResultText = generateBuildingFlavorText(currentLocation);
            }
            else if (currentLocationType.Contains("Room"))
            {
                message.ResultText = generateRoomFlavorText(currentLocation);
            }
            else
            {
                message.ErrorText = "Error";
            }
            return message;
        }



        private string generateRoomFlavorText(Location room)
        {
            var input = room.DescriptionPromptInput();
            room.Description = input.ToString();
            WorldContextService.ContextNodes.Add(room);

            var prompt = _promptService.GenerateSummaryDescription(input.ToString(), SummaryTypeEnum.Room);
            var roomOutput = _aiService.Complete(prompt).Result;
            var peopleInsideRoom = _locationRepository.GetNpcsAtLocation(room);

            var npcInput = new StringBuilder($"Location: {roomOutput}\n");
            foreach (var npc in peopleInsideRoom)
            {
                npc.Description = npc.DescriptionPromptInput().ToString();
                WorldContextService.ContextNodes.Add(npc);
                npcInput.Append(npc.Description);
            }

            var npcPrompt = _promptService.GenerateSummaryDescription(npcInput.ToString(), SummaryTypeEnum.Npcs);
            var npcOutput = _aiService.Complete(npcPrompt).Result;

            var connectingRooms = _locationRepository.GetConnectingRooms(room);
            var connectingRoomsInput = new StringBuilder();
            foreach (var r in connectingRooms)
            {
                r.Description = r.ShortDescriptionPrompt().ToString();
                WorldContextService.ContextNodes.Add(r);
                connectingRoomsInput.Append(r.Description);
            }
            var connectingRoomPrompt = _promptService.GenerateSummaryDescription(connectingRoomsInput.ToString(), SummaryTypeEnum.ConnectingRooms);
            var connectingRoomOutput = _aiService.Complete(connectingRoomPrompt).Result;


            var items = _itemRepository.GetItemsAtLocation(room.Id);
            var itemsString = new StringBuilder("Items in room:");
            foreach (var item in items)
            {
                itemsString.Append($"Name: {item.Name}");
            }


            return roomOutput + npcOutput + connectingRoomOutput + itemsString.ToString();
        }

        private string generateRoadFlavorText(Location road)
        {
            var nearbyBuildings = _locationRepository.GetNearbyBuildings(road);
            var nearbyRoads = _locationRepository.GetNearbyRoads(road);
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

            return _aiService.Complete(prompt).Result;
        }

        private string generateBuildingFlavorText(Location location)
        {
            var building = _buildingRepository.Single(x => x.Id == location.Id).Result;
            var input = building.DescriptionPromptInput();
            var roomPrompt = _promptService.GenerateSummaryDescription(input.ToString(), SummaryTypeEnum.Building);
            var roomOutput = _aiService.Complete(roomPrompt).Result;

            var peopleInsideRoom = _locationRepository.GetNpcsAtLocation(location);
            var npcInput = _promptService.GenerateSummaryDescription(input.ToString(), SummaryTypeEnum.Npcs);
            foreach (var npc in peopleInsideRoom)
            {
                npcInput.Input.Concat(npc.DescriptionPromptInput().ToString());
            }
            var npcPrompt = _promptService.GenerateSummaryDescription(npcInput.ToString(), SummaryTypeEnum.Npcs);
            var npcOutput = _aiService.Complete(roomPrompt).Result;


            return roomOutput + npcOutput;
        }


    }
}
