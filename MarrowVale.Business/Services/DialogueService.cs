using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Common;
using MarrowVale.Data.Contracts;
using MarrowVale.Common.Contracts;
using System.Collections.Generic;
using Neo4jClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class DialogueService : IDialogueService
    {
        private readonly IAiService _aiService;
        private readonly IPromptService _promptService;
        private readonly IPrintService _printService;
        private readonly INpcRepository _npcRepository;
        private readonly IWorldContextService _worldContextService;
        private readonly INpcActionService _npcActionService;
        
        public DialogueService(IAiService aiService, IPromptService promptService, IPrintService printService, INpcRepository npcRepository,
                                IWorldContextService worldContextService, INpcActionService npcActionService)
        {
            _aiService = aiService;
            _promptService = promptService;
            _printService = printService;
            _npcRepository = npcRepository;
            _worldContextService = worldContextService;
            _npcActionService = npcActionService;
        }

        public async Task Converse(Player player, Npc npc)
        {
            npc = await _npcRepository.GetById(npc.Id);
            var transcript = new StringBuilder();
            IReadOnlyList<ChatTurn> conversation = null;
            bool conversationEnded = false;
            _printService.Print("");

            while (!conversationEnded)
            {
                var playerText = _printService.ReadDialogue(player);
                if (playerText == null) return;
                transcript.Append($"Player:{playerText}");

                var responseType = await DetermineResponseType(playerText);

                if (responseType == DialogueResopnseTypeEnum.Purchase)
                {
                    await _npcActionService.PurchaseItem(npc, player, playerText);
                    break;
                }

                if (responseType == DialogueResopnseTypeEnum.Sell)
                {
                    await _npcActionService.SellItem(npc, player, playerText);
                    break;
                }

                (conversation, var response) = await RespondToPlayer(player, playerText, npc, responseType, conversation);

                if (responseType == DialogueResopnseTypeEnum.Farewell)
                    conversationEnded = true;

                transcript.AppendNewLine(string.Empty);
                transcript.AppendNewLine($"{npc.FullName}:{response}");
                _printService.PrintDialogueResponse(npc, response);
            }

            var summary = await SummarizeConversation(transcript.ToString());
            await _npcRepository.CreateMemory(npc, player, summary);
        }

        private async Task<DialogueResopnseTypeEnum> DetermineResponseType(string text)
        {
            var prompt = _promptService.GenerateDialogueIntentPrompt(text, DialogueTypeEnum.InterpretIntent);
            var responseType = (await _aiService.Complete(prompt))?.Trim()?.ToUpper();

            return responseType switch
            {
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Question) => DialogueResopnseTypeEnum.Answer,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Introduction) => DialogueResopnseTypeEnum.Introduction,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Statement) => DialogueResopnseTypeEnum.Statement,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Farewell) => DialogueResopnseTypeEnum.Farewell,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Attack) => DialogueResopnseTypeEnum.Attack,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Purchase) => DialogueResopnseTypeEnum.Purchase,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Sell) => DialogueResopnseTypeEnum.Sell,

                _ => DialogueResopnseTypeEnum.Confused
            };
        }

        private bool IsResponseOfType(string responseType, DialogueResopnseTypeEnum dialogueResponseType)
        {
            return responseType == dialogueResponseType.ToString().ToUpper();
        }


        private async Task<(IReadOnlyList<ChatTurn> conversation, string response)> RespondToPlayer(Player player, string playerText, Npc npc, DialogueResopnseTypeEnum responseType, IReadOnlyList<ChatTurn> currentConversation)
        {
            var context = await generateContext(player, playerText, npc, responseType);
            var character = npc;
            var input = new StringBuilder();
            input.AppendNewLine($"{character.FullName} is a {character.Sentiment} {character.Race} {character.Occupation}.  {context}");

            var dialoguePrompt = _promptService.GenerateDialoguePrompt(input.ToString(), responseType, $"{character.FullName}:");
            var systemMessage = dialoguePrompt.ToString();

            (currentConversation, var response) = await _aiService.SendChat(playerText, systemMessage, currentConversation, responseType.ToString());

            return (currentConversation, response);

        }

        private Task<string> SummarizeConversation(string conversation)
        {
            var summary = _promptService.GenerateDialogueSummaryPrompt(conversation);
            return _aiService.Complete(summary);
        }

        private async Task<string> memoryOfPlayer(Npc c, Player player)
        {
            var conversation = await _npcRepository.MemoryofPlayer(c, player);
            return conversation != null ? $"In their last conversation {conversation.Summary}" : null;
        }

        private async Task<string> generateContext(Player player, string conversation, Npc character, DialogueResopnseTypeEnum responseType)
        {
            if (responseType == DialogueResopnseTypeEnum.Introduction)
            {
                var memory = await memoryOfPlayer(character, player);
                if (memory != null) return memory;
            }
            var characterKnowledge = await _npcRepository.AllKnowledge(character);
            var triggeredMemory = await _worldContextService.ContextSearch(conversation, characterKnowledge);

            if (triggeredMemory != null)
            {
                return $"{triggeredMemory.Name}: {triggeredMemory.Description}";
            }
            if (responseType == DialogueResopnseTypeEnum.Introduction)
                return character.Objective;

            return null;
        }

    }
}
