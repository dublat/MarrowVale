using MarrowVale.Business.Contracts;
using MarrowVale.Common.Contracts;
using MarrowVale.Common.Evaluator;
using MarrowVale.Common.Prompts;
using MarrowVale.Data.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class AiService : IAiService
    {
        private readonly IOpenAiProvider _openAiProvider;
        private readonly IAiEvaluationService _aiEvaluatorService;
        private readonly IOpenAiSettingRepository _openAiSettingRepository;
        private readonly IAppSettingsProvider _config;

        public AiService(IOpenAiProvider openAiProvider, IAiEvaluationService aiEvaluatorService, IOpenAiSettingRepository openAiSettingRepository, IAppSettingsProvider config)
        {
            _config = config;
            _openAiProvider = openAiProvider;
            _aiEvaluatorService = aiEvaluatorService;
            _openAiSettingRepository = openAiSettingRepository;
        }

        private ChatOptions Options(OpenAiSettings s)
        {
            var model = s.EngineName;
            if (string.IsNullOrWhiteSpace(model) || model.StartsWith("text-", StringComparison.OrdinalIgnoreCase)
                || model is "curie" or "babbage" or "davinci" or "ada")
                model = _config.ChatModel;
            var stop = s.MultipleStopSequences?.Length > 0 ? s.MultipleStopSequences :
                string.IsNullOrEmpty(s.StopSequence) ? null : new[] { s.StopSequence };
            return new(model, s.MaxTokens, s.Temperature, s.TopP, s.PresencePenalty, s.FrequencyPenalty, stop, s.Logprobs);
        }

        private async Task<string> Request(IReadOnlyList<ChatTurn> messages, OpenAiSettings settings,
            string api, string type, string subtype)
        {
            var result = await _openAiProvider.Complete(messages, Options(settings));
            await _aiEvaluatorService.CreateEvaluation(result, api, string.Join("\n", messages.Select(m => m.Content)), type, subtype, settings);
            return result.Text;
        }

        public Task<string> Complete(string prompt) =>
            Request(new[] { new ChatTurn("user", prompt) }, new OpenAiSettings(35, .65, presencePenalty: .6),
                "Completion", "Completion", "Raw");

        public async Task<string> Complete(StandardPrompt prompt) =>
            await Request(new[] { new ChatTurn("user", prompt.ToString()) },
                await _openAiSettingRepository.GetSetting(prompt.Type, prompt.SubType) ?? new OpenAiSettings(),
                "Completion", prompt.Type, prompt.SubType);

        public async Task<(IReadOnlyList<ChatTurn> conversation, string response)> SendChat(string message,
            string systemMessage, IReadOnlyList<ChatTurn> conversation = null, string subtype = "Chat")
        {
            var history = conversation?.TakeLast(14).ToList() ?? new List<ChatTurn>();
            history.Add(new("user", message));
            var response = await Request(history.Prepend(new ChatTurn("system", systemMessage)).ToList(),
                await _openAiSettingRepository.GetSetting(MarrowVale.Common.Constants.PromptTypes.DialogueResponse, subtype) ?? new OpenAiSettings(), "Chat", MarrowVale.Common.Constants.PromptTypes.DialogueResponse, subtype);
            history.Add(new("assistant", response));
            return (history, response);
        }

        public async Task<string> Search(IEnumerable<string> documents, string searchTerm)
        {
            var result = await _openAiProvider.SemanticSearch(documents, searchTerm);
            if (result == null) return null;
            await _aiEvaluatorService.CreateEvaluation(result, "Embeddings", searchTerm, "Context", "Search", new OpenAiSettings());
            return result.Text;
        }
    }
}
