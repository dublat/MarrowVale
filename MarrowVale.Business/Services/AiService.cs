using MarrowVale.Business.Contracts;
using MarrowVale.Common.Contracts;
using MarrowVale.Common.Evaluator;
using MarrowVale.Common.Prompts;
using MarrowVale.Data.Contracts;
using OpenAI_API;
using OpenAI_API.Completions;
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
        private readonly string[] stopOn = new string[] { "\n" };

        public AiService(IOpenAiProvider openAiProvider, IAiEvaluationService aiEvaluatorService, IOpenAiSettingRepository openAiSettingRepository)
        {
            _openAiProvider = openAiProvider;
            _aiEvaluatorService = aiEvaluatorService;
            _openAiSettingRepository = openAiSettingRepository;
        }

        public async Task<string> BestMatch(string item, string options)
        {
            throw new NotImplementedException();
        }

        //Refactor to use StandardPrompt
        public async Task<string> Complete(string prompt)
        {
            var request = new CompletionRequest(prompt, model: OpenAI_API.Models.Model.CurieText, temperature: 0.65, max_tokens: 35, frequencyPenalty: 0, presencePenalty: .6, stopSequences: stopOn);
            var completionResult = await _openAiProvider.Complete(request);

            return completionResult.Completions.First().Text;
        }

        public async Task<string> Complete(StandardPrompt prompt)
        {
            var settings = _openAiSettingRepository.GetSetting(prompt.Type, prompt.SubType);
            var completionRequest = createCompletionRequest(settings, prompt.ToString());
            var result = await _openAiProvider.Complete(completionRequest);

            var apiName = "Completion";
            await _aiEvaluatorService.CreateEvaluation(result, apiName, prompt, settings);
            return result.ToString();
        }

        public async Task<string> Search(IEnumerable<string> documents, string searchTerm)
        {
            return await _openAiProvider.SemanticSearch(documents, searchTerm);

        }

        private CompletionRequest createCompletionRequest(OpenAiSettings settings, string prompt)
        {
            return new CompletionRequest(prompt, 
                                        temperature: settings.Temperature, 
                                        max_tokens: settings.MaxTokens, 
                                        frequencyPenalty: settings.FrequencyPenalty, 
                                        presencePenalty: settings.PresencePenalty, 
                                        stopSequences: stopOn);
        }
    }
}
