using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Common;
using MarrowVale.Data.Contracts;
using Neo4jClient;
using OpenAI_API.Chat;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class DialogueService : IDialogueService
    {
        private readonly IAiService _aiService;
        private readonly IGraphClient _graphClient;
        private readonly IPromptService _promptService;
        private readonly IPrintService _printService;
        private readonly INpcRepository _npcRepository;
        private readonly IWorldContextService _worldContextService;
        private readonly INpcActionService _npcActionService;
        
        public DialogueService(IAiService aiService, IGraphClient graphClient, IPromptService promptService, IPrintService printService, INpcRepository npcRepository,
                                IWorldContextService worldContextService, INpcActionService npcActionService)
        {
            _aiService = aiService;
            _graphClient = graphClient;
            _promptService = promptService;
            _printService = printService;
            _npcRepository = npcRepository;
            _worldContextService = worldContextService;
            _npcActionService = npcActionService;
        }

        public async Task Converse(Player player, Npc npc)
        {
            npc = await _npcRepository.GetById(npc.Id);
            var fullTextConverstaion = new StringBuilder();
            Conversation conversation = null;
            bool conversationEnded = false;
            _printService.Print("");

            while (!conversationEnded)
            {
                var playerText = _printService.ReadDialogue(player);
                fullTextConverstaion.Append($"Player:{playerText}");

                var responseType = DetermineReponseType(playerText);

                if (responseType == DialogueResopnseTypeEnum.Purchase)
                {
                    _npcActionService.PurchaseItem(npc, player, playerText);
                    break;
                }

                if (responseType == DialogueResopnseTypeEnum.Sell)
                {
                    _npcActionService.SellItem(npc, player, playerText);
                    break;
                }

                (conversation, var response) = await RespondToPlayer(playerText, npc, responseType, fullTextConverstaion.ToString(), conversation);

                if (responseType == DialogueResopnseTypeEnum.Farewell)
                    conversationEnded = true;

                fullTextConverstaion.AppendNewLine(string.Empty);
                fullTextConverstaion.AppendNewLine($"{npc.FullName}:{response}");
                _printService.PrintDialogueResponse(npc, response);
            }

            var summary = summarizeConverstion(fullTextConverstaion.ToString(), npc);
            await _npcRepository.CreateMemory(npc, summary);
        }

        private DialogueResopnseTypeEnum DetermineReponseType(string text)
        {
            var prompt = _promptService.GenerateDialogueIntentPrompt(text, DialogueTypeEnum.InterpretIntent);
            var responseType = _aiService.Complete(prompt).Result?.Trim()?.ToUpper();

            return responseType switch
            {
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Question) => DialogueResopnseTypeEnum.Answer,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Introduction) => DialogueResopnseTypeEnum.Introduction,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Statement) => DialogueResopnseTypeEnum.Statement,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Farewell) => DialogueResopnseTypeEnum.Farewell,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Attack) => DialogueResopnseTypeEnum.Attack,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Purchase) => DialogueResopnseTypeEnum.Purchase,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Sell) => DialogueResopnseTypeEnum.Sell,
                var res when IsResponseOfType(res, DialogueResopnseTypeEnum.Give) => DialogueResopnseTypeEnum.Give,
                _ => DialogueResopnseTypeEnum.Confused
            };
        }

        private bool IsResponseOfType(string responseType, DialogueResopnseTypeEnum dialogueResponseType)
        {
            return responseType == dialogueResponseType.ToString().ToUpper();
        }


        private async Task<(Conversation conversation, string response)> RespondToPlayer(string playerText, Npc npc, DialogueResopnseTypeEnum responseType, string fullTextConversation, Conversation currentConversation)
        {
            var context = generateContext(fullTextConversation, npc, responseType);
            var character = (await _graphClient.Cypher
                                .Match("(character:Character)")
                                .Where((Npc character) => character.Name == npc.Name)
                                .Return(character => character.As<Npc>())
                                .ResultsAsync).FirstOrDefault();

            var input = new StringBuilder();
            input.AppendNewLine($"{character.FullName} is a {character.Sentiment} {character.Race} {character.Occupation}.  {context}");
            input.Append(fullTextConversation);
            var dialoguePrompt = _promptService.GenerateDialoguePrompt(input.ToString(), responseType, $"{character.FullName}:");
            var systemMessage = dialoguePrompt.ToString();

            (currentConversation, var response) = await _aiService.SendChat(playerText, systemMessage, currentConversation);

            return (currentConversation, response);

        }

        private string summarizeConverstion(string conversation, Npc Character)
        {
            var summary = _promptService.GenerateDialogueSummaryPrompt(conversation);
            return _aiService.Complete(summary).Result;
        }

        private string memoryOfPlayer(Npc c)
        {
            var conversation = _npcRepository.MemoryofPlayer(c);
            return conversation != null ? $"In their last conversation {conversation.Summary}" : null;
        }

        private string generateContext(string conversation, Npc character, DialogueResopnseTypeEnum responseType)
        {
            var characterKnowledge = _npcRepository.AllKnowledge(character);
            var triggeredMemory =_worldContextService.ContextSearch(conversation, characterKnowledge);

            if (triggeredMemory != null)
            {
                var memoryContext = $"{character.Name} {triggeredMemory?.RelationshipLabel?.FirstOrDefault()} ";
                memoryContext = memoryContext + string.Join(", ",triggeredMemory.Relations.Select(x => x.Node.Name));
                 
                return memoryContext;
            }


            if (responseType == DialogueResopnseTypeEnum.Introduction)
                return memoryOfPlayer(character) ?? character.Objective;
            else if (responseType == DialogueResopnseTypeEnum.Farewell)
            { 
            
            }
            else if (responseType == DialogueResopnseTypeEnum.Statement)
            {

            }
            else if (responseType == DialogueResopnseTypeEnum.Attack)
            {

            }
            else if (responseType == DialogueResopnseTypeEnum.Confused)
            {

            }

            return null;
        }

    }
}
