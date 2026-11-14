using System;
using System.IO;
using System.Threading.Tasks;
using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.PathObstacles;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Business.Services;
using MarrowVale.Common.Contracts;
using MarrowVale.Common.Prompts;
using MarrowVale.Data.Contracts;
using MarrowVale.Data.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace MarrowVale.Business.Tests;

public class GameFlowTests
{
    [Fact]
    public async Task GameWaitsForOpeningDescriptionBeforeReadingQuit()
    {
        var player = new Player();
        var setup = new Mock<IGameSetupService>();
        setup.Setup(s => s.Setup()).ReturnsAsync(player);
        var opening = new TaskCompletionSource();
        var commands = new Mock<ICommandProcessingService>();
        commands.Setup(c => c.ProcessCommand(It.Is<Command>(cmd => cmd.Type == CommandEnum.LookAround), player)).Returns(opening.Task);
        var print = new Mock<IPrintService>();
        print.Setup(p => p.ReadInput()).Returns("QUIT");
        var game = new GameService(print.Object, setup.Object, Mock.Of<IGameRepository>(), null, commands.Object);
        var session = game.Start();
        print.Verify(p => p.ReadInput(), Times.Never);
        opening.SetResult();
        await session;
        print.Verify(p => p.ReadInput(), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OpenHonorsDoorLock(bool unlocked)
    {
        var door = new Door { Id = "door", IsUnlocked = unlocked };
        var doors = new Mock<IDoorRepository>();
        doors.Setup(d => d.GetById(door.Id)).ReturnsAsync(door);
        var service = new EnviornmentalInteractionService(null, null, doors.Object, null);
        var message = await service.OpenDoor(new Player(), new Command { DirectObjectNode = door });
        doors.Verify(d => d.OpenDoor(door), unlocked ? Times.Once() : Times.Never());
        Assert.Equal(unlocked, !string.IsNullOrEmpty(message.ResultText));
    }

    [Theory]
    [InlineData("999", "Unrecognized command.")]
    [InlineData("attack then dance", "Unrecognized command.")]
    [InlineData("Dance", "Dance is unavailable.")]
    public async Task InvalidOrUnimplementedIntentReturnsControl(string answer, string expected)
    {
        var ai = new Mock<IAiService>();
        ai.Setup(a => a.Complete(It.IsAny<StandardPrompt>())).ReturnsAsync(answer);
        var print = new Mock<IPrintService>();
        var service = new InputProcessingService(NullLoggerFactory.Instance, print.Object, ai.Object, null, Mock.Of<IPromptService>(), null);
        Assert.Null(await service.ProcessInput("action", "", new Player()));
        print.Verify(p => p.Print(expected, 0), Times.Once());
        print.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MissingDestinationDoesNotMovePlayer()
    {
        var player = new Player();
        var players = new Mock<IPlayerRepository>();
        players.Setup(p => p.GetPlayerLocation(player)).ReturnsAsync(new Location());
        var service = new NavigationService(players.Object, null, Mock.Of<ILocationRepository>(), null, null, null, null, null, null);
        var message = await service.TraversePath(new Command { DirectObjectNode = new Road { Id = "missing" } }, player);
        Assert.False(string.IsNullOrEmpty(message.ErrorText));
        players.Verify(p => p.MovePlayer(It.IsAny<Player>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData("Y", true)]
    [InlineData("N", false)]
    public async Task MerchantRequiresPurchaseConfirmation(string confirmation, bool purchased)
    {
        var player = new Player();
        var npc = new Npc();
        var item = new Item { Name = "torch", BaseWorth = 5 };
        var context = new Mock<IWorldContextService>();
        context.Setup(c => c.DirectObjectCommand<Item>("buy torch", null, npc, CommandEnum.Purchase)).ReturnsAsync(item);
        var repository = new Mock<INpcRepository>();
        var print = new Mock<IPrintService>();
        print.Setup(p => p.ReadInput()).Returns(confirmation);
        var merchant = new NpcActionService(null, null, print.Object, repository.Object, context.Object, null);
        await merchant.PurchaseItem(npc, player, "buy torch");
        repository.Verify(r => r.BuyItem(npc, player, item, 5, 1), purchased ? Times.Once() : Times.Never());
    }

    [Fact]
    public async Task CombatFleeEndsBeforeEnemyTurn()
    {
        var print = new Mock<IPrintService>();
        print.Setup(p => p.ReadInput()).Returns("run");
        var ai = new Mock<IAiService>();
        ai.Setup(a => a.Complete(It.IsAny<StandardPrompt>())).ReturnsAsync("Flee");
        var combat = new CombatService(NullLoggerFactory.Instance, print.Object, null, Mock.Of<IPromptService>(), ai.Object, null, null, null);
        await combat.Attack(new Player { CurrentHealth = 10 }, new Npc { IsAlive = true, CurrentHealth = 10 });
        print.Verify(p => p.ClearConsole(), Times.Once());
    }

    [Fact]
    public void ExistingSaveSurvivesWritingNewSlot()
    {
        var directory = Directory.CreateTempSubdirectory();
        try
        {
            var settings = Mock.Of<IAppSettingsProvider>(s => s.DataFilesLocation == directory.FullName);
            var repository = new GameRepository(NullLoggerFactory.Instance, settings);
            File.WriteAllText(Path.Combine(directory.FullName, "old.json"), "{\"GameTime\":{\"TimeOfDay\":23,\"DaysElapsed\":4}}");
            var game = repository.LoadGame("old");
            game.GameTime.IncrementTime();
            repository.SaveGame(game, "old", "new");
            Assert.Equal(5, repository.LoadGame("new").GameTime.DaysElapsed);
            Assert.Equal(23, repository.LoadGame("old").GameTime.TimeOfDay);
        }
        finally { directory.Delete(true); }
    }
}
