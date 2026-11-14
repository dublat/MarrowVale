using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.Graph;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Business.Services;
using MarrowVale.Common.Contracts;
using MarrowVale.Common.Evaluator;
using MarrowVale.Data.Contracts;
using Moq;
using Xunit;

namespace MarrowVale.Business.Tests;

public class NpcTests
{
    [Fact]
    public async Task FarewellSummarizesDialogueForTheCurrentPlayer()
    {
        var player = new Player();
        var npc = new Npc { Id = "npc", Name = "Mira" };
        var repository = new Mock<INpcRepository>();
        repository.Setup(r => r.GetById("npc")).ReturnsAsync(npc);
        var print = new Mock<IPrintService>();
        print.Setup(p => p.ReadDialogue(player)).Returns("goodbye");
        var prompts = new Mock<IPromptService>();
        var replyPrompt = new MarrowVale.Common.Prompts.StandardPrompt { Examples = new(), Input = "NPC persona" };
        prompts.Setup(p => p.GenerateDialoguePrompt(It.IsAny<string>(), DialogueResopnseTypeEnum.Farewell, It.IsAny<string>())).Returns(replyPrompt);
        var ai = new Mock<IAiService>();
        ai.SetupSequence(a => a.Complete(It.IsAny<MarrowVale.Common.Prompts.StandardPrompt>())).ReturnsAsync("Farewell").ReturnsAsync("They parted.");
        ai.Setup(a => a.SendChat("goodbye", It.Is<string>(system => !system.Contains("goodbye")), null, "Farewell"))
            .ReturnsAsync((Array.Empty<ChatTurn>(), "Safe travels."));
        var service = new DialogueService(ai.Object, prompts.Object, print.Object, repository.Object, Mock.Of<IWorldContextService>(), null);
        await service.Converse(player, npc);
        print.Verify(p => p.PrintDialogueResponse(npc, "Safe travels."), Times.Once);
        prompts.Verify(p => p.GenerateDialogueSummaryPrompt(It.Is<string>(text => text.Contains("goodbye") && text.Contains("Safe travels."))));
        repository.Verify(r => r.CreateMemory(npc, player, "They parted."), Times.Once);
    }

    [Fact]
    public async Task ConversationFinishesBeforeDispatchPrintsCompletion()
    {
        var player = new Player();
        var npc = new Npc { Id = "npc" };
        var repository = new Mock<INpcRepository>();
        repository.Setup(r => r.GetById(npc.Id)).ReturnsAsync(npc);
        repository.Setup(r => r.IsPlayerNearby(player, npc)).ReturnsAsync(true);
        var done = new TaskCompletionSource();
        var dialogue = new Mock<IDialogueService>();
        dialogue.Setup(d => d.Converse(player, npc)).Returns(done.Task);
        var print = new Mock<IPrintService>();
        var dispatch = new CommandProcessingService(null, null, dialogue.Object, repository.Object, null, null, null, print.Object, null, null);
        var turn = dispatch.ProcessCommand(new Command { Type = CommandEnum.Speak, DirectObjectNode = npc }, player);
        Assert.False(turn.IsCompleted);
        print.VerifyNoOtherCalls();
        done.SetResult();
        await turn;
        print.Verify(p => p.Type("Conversation Over", 0, false), Times.Once);
    }

    [Fact]
    public async Task CyclicKnowledgeFindsRelatedNode()
    {
        var npc = new GraphNode { Name = "merchant" };
        var shop = new GraphNode { Name = "shop" };
        npc.Relations = new[] { new BaseRelationship { Relation = "OWNS", Node = shop } };
        shop.Relations = new[] { new BaseRelationship { Relation = "HAS", Node = npc } };
        var ai = new Mock<IAiService>(MockBehavior.Strict);
        ai.Setup(a => a.Search(It.Is<IEnumerable<string>>(docs => docs.Contains("merchant OWNS shop")), "store"))
            .ReturnsAsync("merchant OWNS shop");
        var context = new WorldContextService(null, null, ai.Object, null, null, null, null, null, null);
        Assert.Null(await context.ContextSearch("store", (GraphNode)null));
        Assert.Same(shop, await context.ContextSearch("store", npc));
    }

    [Fact]
    public async Task ChatKeepsEightTurnsAndOneSystemMessage()
    {
        var provider = new Mock<IOpenAiProvider>();
        var evaluator = new Mock<IAiEvaluationService>();
        provider.Setup(p => p.Complete(It.IsAny<IReadOnlyList<ChatTurn>>(), It.IsAny<ChatOptions>()))
            .Callback<IReadOnlyList<ChatTurn>, ChatOptions>((messages, options) =>
            {
                Assert.Equal(new ChatTurn("system", "persona"), messages.First());
                Assert.Single(messages.Where(m => m.Role == "system"));
                Assert.Equal("user", messages.Last().Role);
                Assert.InRange(messages.Count, 2, 16);
            }).ReturnsAsync(new AiResult("reply", "fixture", 7, TimeSpan.Zero));
        var service = new AiService(provider.Object, evaluator.Object, Mock.Of<IOpenAiSettingRepository>(), Mock.Of<IAppSettingsProvider>());
        IReadOnlyList<ChatTurn> history = null;
        for (var i = 0; i < 12; i++)
            (history, _) = await service.SendChat(i.ToString(), "persona", history);
        Assert.Equal(16, history.Count);
        Assert.Equal("4", history.First().Content);
        Assert.Equal(12, evaluator.Invocations.Count);
    }
}
