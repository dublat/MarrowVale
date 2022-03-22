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


        public void ProcessCommand(Command command, Player player)
        {
            do
            {
                var message = ProcessCommandMap(command, player);
                if (!string.IsNullOrWhiteSpace(message.ResultText)) 
                    _printService.Type(message.ResultText);
                else if (!string.IsNullOrWhiteSpace(message.ErrorText))
                    _divineInterventionService.HandleError(command.Input, error: message.ErrorText);

                command = message.NextCommand;
            }
            while (command != null);


        }


        private MarrowValeMessage ProcessCommandMap(Command command, Player player) =>
            command?.Type switch
            {
                CommandEnum.Enter => _navigationService.Enter(command, player),
                CommandEnum.Traverse => _navigationService.TraversePath(command, player),
                CommandEnum.Speak => speak(command, player),
                CommandEnum.Inventory => inspectInventory(player),
                CommandEnum.Health => inspectHealth(player),
                CommandEnum.Attack => attack(command, player),
                CommandEnum.Exit => _navigationService.Exit(command, player),
                CommandEnum.LookAround => _navigationService.CurrentLocationDescription(player),
                CommandEnum.Take => _enviornmentalInteractionService.PickUpItem(command, player),
                CommandEnum.Drop => _enviornmentalInteractionService.DropItem(command, player),
                CommandEnum.Lock => _enviornmentalInteractionService.LockDoor(player, command),
                CommandEnum.Unlock => _enviornmentalInteractionService.UnlockDoor(player, command),
                CommandEnum.Open => _enviornmentalInteractionService.OpenDoor(player, command),
                CommandEnum.Close => _enviornmentalInteractionService.CloseDoor(player, command),
                _ => new MarrowValeMessage { ErrorText = "Command Is Not Mapped to a function" }
            };


        private MarrowValeMessage speak(Command command, Player player)
        {
            var message = new MarrowValeMessage();
            var npc = _npcRepository.Single(x => x.Id == command.DirectObjectNode.Id).Result;
            if (isAbleToSpeakWith(player, npc))
            {
                _dialogueService.Talk(player, npc);
                message.ResultText = "Conversation Over";
            }
            else
            {
                message.ErrorText = $"Unable to speak with character {npc?.FullName ?? command?.DirectObjectNode?.Name}";
            }

            return message;
        }


        private bool isAbleToSpeakWith(Player player, Npc npc)
        {
            return _npcRepository.IsPlayerNearby(player, npc);
        }
        private bool isAbleToAttack(Player player, Npc npc)
        {
            return _npcRepository.IsPlayerNearby(player, npc);
        }

        private MarrowValeMessage inspectInventory(Player player)
        {
            var message = new MarrowValeMessage();

            var inventory = _playerRepository.GetInventory(player);
            foreach (var item in inventory.Items)
            {
                Console.WriteLine(item.GetShortDescription());
            }

            message.ResultText = $"{player.Name}'s capicity is";
            return message;
        }

        private MarrowValeMessage inspectHealth(Player p)
        {
            var message = new MarrowValeMessage();
            var player = _playerRepository.GetById(p.Id).Result;
            message.ResultText = $"You are at {player.CurrentHealth}/{player.MaxHealth} health";

            return message;
        }

        private MarrowValeMessage attack(Command command, Player player)
        {
            var message = new MarrowValeMessage();

            var npc = _npcRepository.GetById(command.DirectObjectNode.Id).Result;
            _npcRepository.SetCombatEquipment(npc);

            if (isAbleToAttack(player, npc))
            {
                message.ResultText = _combatService.Attack(player, npc);
            }
            else
            {
                message.ErrorText = _divineInterventionService.HandleError(command.Input, $"Unable to attack character {player?.Name ?? command?.DirectObjectNode?.Name}");
            }

            return message;
        }

    }
}
