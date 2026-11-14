using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class InputProcessingService : IInputProcessingService
    {
        private readonly ILogger _logger;
        private readonly IPrintService _printService;
        private readonly IAiService _aiService;
        private readonly IWorldContextService _worldContextService;
        private readonly IPromptService _promptService;
        private readonly IDivineInterventionService _divineInterventionService;

        public InputProcessingService(ILoggerFactory logger, IPrintService printService, IAiService aiService, 
            IWorldContextService worldContextService, IPromptService promptService, IDivineInterventionService divineInterventionService)
        {
            _logger = logger.CreateLogger<InputProcessingService>();
            _printService = printService;
            _aiService = aiService;
            _worldContextService = worldContextService;
            _promptService = promptService;
            _divineInterventionService = divineInterventionService;
        }

        public async Task<Command> ProcessInput(string input, string context, Player player)
        {
            var prompt = _promptService.CommandTypePrompt(input);            
            var commandName = await _aiService.Complete(prompt);
            var name = commandName?.Trim();
            if (!Enum.TryParse(name, true, out CommandEnum command) ||
                !Enum.IsDefined(command) || !string.Equals(command.ToString(), name, StringComparison.OrdinalIgnoreCase))
            {
                _printService.Print("Unrecognized command.");
                return null;
            }
            if (command is CommandEnum.Health or CommandEnum.LookAround or CommandEnum.Inventory)
                return new Command(command);
            if (command is CommandEnum.Enter or CommandEnum.Exit or CommandEnum.Traverse or CommandEnum.Attack
                or CommandEnum.Speak or CommandEnum.Take or CommandEnum.Drop or CommandEnum.Unlock
                or CommandEnum.Lock or CommandEnum.Open or CommandEnum.Close)
            {
                var objectPrompt = _promptService.GenerateDirectObjectPrompt(input, command);
                var target = await _aiService.Complete(objectPrompt.ToString());
                var node = await _worldContextService.GetObjectLabelIdPair(target.Trim(), player, input);
                if (string.IsNullOrWhiteSpace(node?.Id))
                {
                    _printService.PrintDivineText(await _divineInterventionService.HandleError(input, "Not found"));
                    return null;
                }
                return new Command { Type = command, DirectObjectNode = node, Input = input };
            }
            _printService.Print($"{command} is unavailable.");
            return null;
        }
    }
}
