using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Data.Contracts;
using MarrowVale.Data.Seeder;
using System;
using System.Threading;

namespace MarrowVale.Business.Services
{
    public class GameSetupService : IGameSetupService
    {
        private readonly ICharacterService _characterService;
        private readonly IPrintService _printService;
        private readonly IDrawingRepository _drawingRepository;
        private readonly IDrawingService _drawingService;
        private readonly IEntityGenerator _entityGenerator;

        public GameSetupService(ICharacterService characterService, IDrawingRepository drawingRepository, IPrintService printService,
            IDrawingService drawingService, IEntityGenerator entityGenerator)
        {
            _characterService = characterService;
            _printService = printService;
            _drawingRepository = drawingRepository;
            _drawingService = drawingService;
            _entityGenerator = entityGenerator;
        }

        public async System.Threading.Tasks.Task<Player> Setup()
        {
            while (true)
            {
                var title = _drawingRepository.GetTitleArt();
                _drawingService.PrintArtCentered(title);

                _printService.TypeCentered("New Game", 8);
                _printService.TypeCentered("Continue");

                var gameType = _printService.ReadInput();

                if (gameType == null) return null;
                await _entityGenerator.DefaultSettings();

                if (gameType.ToUpper() == "NEW GAME")
                {
                    await _entityGenerator.GenerateBuildings();
                    await _entityGenerator.GenerateCharacters();
                    return await _characterService.NewCharacter();
                }
                else if(gameType.ToUpper() == "CONTINUE")
                {
                    return await _characterService.LoadCharacter();
                }
                else
                {
                    _printService.Print("You must choose to start a New Game or Continue a saved game. Type your choice.");
                    Thread.Sleep(4000);

                }
            }
        }

    }
}
