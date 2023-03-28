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
            var isValidEnum = Enum.TryParse(commandName, out CommandEnum commandEnum);

            if (!isValidEnum)
                return await retryInput(context, player, input);

            return await CreateCommand(input, context, player, commandEnum);

        }


        private async Task<Command> CreateCommand(string input, string context, Player player, CommandEnum command) =>
            command switch
            {
                CommandEnum.Inventory => directObjectCommand(input, context, player, command),
                CommandEnum.Enter => directObjectCommand(input, context, player, command),
                CommandEnum.Exit => directObjectCommand(input, context, player, command),
                CommandEnum.Traverse => directObjectCommand(input, context, player, command),
                CommandEnum.Attack => directObjectCommand(input, context, player, command),
                CommandEnum.Speak => directObjectCommand(input, context, player, command),
                CommandEnum.Health => new Command(CommandEnum.Health),
                CommandEnum.MoveToward => throw new NotImplementedException(),
                CommandEnum.ClimbUp => throw new NotImplementedException(),
                CommandEnum.ClimbDown => throw new NotImplementedException(),
                CommandEnum.Swim => throw new NotImplementedException(),
                CommandEnum.Dance => throw new NotImplementedException(),
                CommandEnum.Give => throw new NotImplementedException(),
                CommandEnum.Take => directObjectCommand(input, context, player, command),
                CommandEnum.Drop => directObjectCommand(input, context, player, command),
                CommandEnum.Unlock => directObjectCommand(input, context, player, command),
                CommandEnum.Lock => directObjectCommand(input, context, player, command),
                CommandEnum.Open => directObjectCommand(input, context, player, command),
                CommandEnum.Close => directObjectCommand(input, context, player, command),
                CommandEnum.Equip => throw new NotImplementedException(),
                CommandEnum.Use => throw new NotImplementedException(),
                CommandEnum.Cast => throw new NotImplementedException(),
                CommandEnum.Abilities => throw new NotImplementedException(),
                //CommandEnum.Read => throw new NotImplementedException(),
                _ => await retryInput(context, player, input)
            };


        private Command directObjectCommand(string input, string context, Player player, CommandEnum command)
        {
            while (true)
            {
                try
                {
                    var prompt = _promptService.GenerateDirectObjectPrompt(input, command);
                    var directObject = _aiService.Complete(prompt.ToString()).Result;
                    var node = _worldContextService.GetObjectLabelIdPair(directObject, player, input);
                    if (string.IsNullOrWhiteSpace(node?.Id))
                    {
                        var divineResponse = _divineInterventionService.HandleError(input, "Not found");
                        Debug.WriteLine("Direct Object Node Not Found");
                        _printService.PrintDivineText(divineResponse);
                        input = _printService.ReadInput();
                    }
                    else
                        return new Command { Type = command, DirectObjectNode = node };
                }
                catch
                {
                    var divineResponse = _divineInterventionService.HandleError(input, "Unexpected Error");
                    _printService.PrintDivineText(divineResponse);
                    input = _printService.ReadInput();
                }

            }
        }

        private async Task<Command> retryInput(string context, Player player, string input)
        {
            var divineResponse = _divineInterventionService.HandleError(input, "Unexpected Command");
            _printService.PrintDivineText(divineResponse);
            input = _printService.ReadInput();
            return await ProcessInput(input, context, player);
        }

    }
}
