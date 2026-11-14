using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Common.Contracts;
using MarrowVale.Data.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class GameService : IGameService
    {
        private Game _game;
        private readonly IPrintService _printService;
        private readonly IGameRepository _gameRepository;
        private readonly IInputProcessingService _inputProcessingService;
        private readonly ICommandProcessingService _commands;
        private readonly IGameSetupService _setup;
        private readonly CancellationToken _cancellation;

        public GameService(IPrintService printService, IGameSetupService setup, IGameRepository gameRepository,
            IInputProcessingService inputProcessingService, ICommandProcessingService commands, CancellationToken cancellation = default)
        {
            _printService = printService;
            _setup = setup;
            _gameRepository = gameRepository;
            _inputProcessingService = inputProcessingService;
            _commands = commands;
            _cancellation = cancellation;
        }

        public async Task Start()
        {
            var player = await _setup.Setup();
            if (player == null) return;
            _game = _gameRepository.LoadGame(player.GameSaveName);
            _printService.ClearConsole();
            var lookAround = new Command { Type = CommandEnum.LookAround };
            await _commands.ProcessCommand(lookAround, player);

            while (true)
            {
                _cancellation.ThrowIfCancellationRequested();
                var playerInput = _printService.ReadInput();
                if (playerInput == null || playerInput == "QUIT")
                    break;

                var command = await _inputProcessingService.ProcessInput(playerInput, "", player);
                if (command != null) await _commands.ProcessCommand(command, player);

            }
        }

    }
}
