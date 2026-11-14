using System.Threading.Tasks;
using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Data.Contracts;
using System;
using System.Linq;


namespace MarrowVale.Business.Services
{
    public class CommandProcessingService : ICommandProcessingService
    {
        private readonly IWorldContextService _worldContextService;
        private readonly IDialogueService _dialogueService;
        private readonly IPlayerRepository _playerRepository;
        private readonly INpcRepository _npcRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly ICombatService _combatService;
        private readonly IDivineInterventionService _divineInterventionService;
        private readonly IPrintService _printService;
        private readonly INavigationService _navigationService;
        private readonly IEnviornmentalInteractionService _enviornmentalInteractionService;

        public CommandProcessingService(IWorldContextService worldContextService, IPlayerRepository playerRepository, IDialogueService dialogueService,
                                        INpcRepository npcRepository, ILocationRepository locationRepository, ICombatService combatService, IDivineInterventionService divineInterventionService,
                                        IPrintService printService, INavigationService navigationService, IEnviornmentalInteractionService enviornmentalInteractionService)
        {
            _worldContextService = worldContextService;
            _playerRepository = playerRepository;
            _dialogueService = dialogueService;
            _npcRepository = npcRepository;
            _locationRepository = locationRepository;
            _combatService = combatService;
            _divineInterventionService = divineInterventionService;
            _printService = printService;
            _navigationService = navigationService;
            _enviornmentalInteractionService = enviornmentalInteractionService;
        }


        public async Task ProcessCommand(Command command, Player player)
        {
            do
            {
                var message = await ProcessCommandMap(command, player);
                if (!string.IsNullOrWhiteSpace(message.ResultText)) 
                    _printService.Type(message.ResultText);
                else if (!string.IsNullOrWhiteSpace(message.ErrorText))
                    _printService.PrintDivineText(await _divineInterventionService.HandleError(command.Input, error: message.ErrorText));

                command = message.NextCommand;
            }
            while (command != null);


        }


        private async Task<MarrowValeMessage> ProcessCommandMap(Command command, Player player) =>
            command?.Type switch
            {
                CommandEnum.Enter => await _navigationService.Enter(command, player),
                CommandEnum.Traverse => await _navigationService.TraversePath(command, player),
                CommandEnum.Speak => await speak(command, player),
                CommandEnum.Inventory => await inspectInventory(player),
                CommandEnum.Health => await inspectHealth(player),
                CommandEnum.Attack => await attack(command, player),
                CommandEnum.Exit => await _navigationService.Exit(command, player),
                CommandEnum.LookAround => await _navigationService.CurrentLocationDescription(player),
                CommandEnum.Take => await _enviornmentalInteractionService.PickUpItem(command, player),
                CommandEnum.Drop => await _enviornmentalInteractionService.DropItem(command, player),
                CommandEnum.Lock => await _enviornmentalInteractionService.LockDoor(player, command),
                CommandEnum.Unlock => await _enviornmentalInteractionService.UnlockDoor(player, command),
                CommandEnum.Open => await _enviornmentalInteractionService.OpenDoor(player, command),
                CommandEnum.Close => await _enviornmentalInteractionService.CloseDoor(player, command),
                _ => new MarrowValeMessage { ErrorText = "Command Is Not Mapped to a function" }
            };


        private async Task<MarrowValeMessage> speak(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var npc = command.DirectObjectNode == null ? null : await _npcRepository.GetById(command.DirectObjectNode.Id);
            if (await isAbleToSpeakWith(player, npc))
            {
                await _dialogueService.Converse(player, npc);
                message.ResultText = "Conversation Over";
            }
            else
            {
                message.ErrorText = $"Unable to speak with character {npc?.FullName ?? command?.DirectObjectNode?.Name}";
            }

            return message;
        }


        private async Task<bool> isAbleToSpeakWith(Player player, Npc npc)
        {
            return await _npcRepository.IsPlayerNearby(player, npc);
        }
        private async Task<bool> isAbleToAttack(Player player, Npc npc)
        {
            return await _npcRepository.IsPlayerNearby(player, npc);
        }

        private async Task<MarrowValeMessage> inspectInventory(Player player)
        {
            var message = new MarrowValeMessage();

            var inventory = await _playerRepository.GetInventory(player);
            foreach (var item in inventory.Items)
            {
                Console.WriteLine(item.GetShortDescription());
            }

            message.ResultText = $"{player.Name}'s capicity is";
            return message;
        }

        private async Task<MarrowValeMessage> inspectHealth(Player p)
        {
            var message = new MarrowValeMessage();
            var player = await _playerRepository.GetById(p.Id);
            message.ResultText = $"You are at {player.CurrentHealth}/{player.MaxHealth} health";

            return message;
        }

        private async Task<MarrowValeMessage> attack(Command command, Player player)
        {
            var message = new MarrowValeMessage();

            var npc = await _npcRepository.GetById(command.DirectObjectNode.Id);


            if (await isAbleToAttack(player, npc))
            {
                await _npcRepository.SetCombatEquipment(npc);
                message.ResultText = await _combatService.Attack(player, npc);
            }
            else
            {
                message.ErrorText = $"Unable to attack character {command?.DirectObjectNode?.Name}";
            }

            return message;
        }

    }
}
