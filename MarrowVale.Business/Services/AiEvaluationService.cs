using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Common.Contracts;
using MarrowVale.Common.Prompts;
using MarrowVale.Data.Contracts;
using System;
using System.Threading.Tasks;

namespace MarrowVale.Common.Evaluator
{
    public class AiEvaluationService : IAiEvaluationService
    {
        private readonly IAppSettingsProvider _appSettingsProvider;
        private readonly IPromptRepository _promptRepository;
        private readonly IOpenAiEvaluationRepository _openAiEvaluationRepository;
        public AiEvaluationService(IAppSettingsProvider appSettingsProvider, 
                                   IPromptRepository promptRepository, IOpenAiEvaluationRepository openAiEvaluationRepository)
        {
            _appSettingsProvider = appSettingsProvider;
            _promptRepository = promptRepository;
            _openAiEvaluationRepository = openAiEvaluationRepository;
        }

        public async Task<Evaluation> CreateEvaluation(AiResult result, string apiName, string prompt, string type, string subtype, OpenAiSettings settings)
        {
            var evaluation = new Evaluation
            {
                RunDate = DateTime.Now,
                ApiName = apiName,
                Engine = result.Model,
                Prompt = prompt.ToString(),
                Response = result.Text,
                ResponseTime = result.Duration,
                FrequencyPenalty = settings.FrequencyPenalty,
                MaxTokens = settings.MaxTokens,
                MultipleStopSequences = settings.MultipleStopSequences,
                PresencePenalty = settings.PresencePenalty,
                Logprobs = settings.Logprobs,
                StopSequence = settings.StopSequence,
                Temperature = settings.Temperature,
                TopP = settings.TopP
            };

            evaluation.TokenCount = result.TotalTokens;
            evaluation.Rating = ratePrompt(prompt, result.Text);


            var promptType = new PromptType { Name = type };
            var promptSubType = new PromptSubType { Name = subtype };
            await _promptRepository.EnsurePromptCreated(promptType, promptSubType);
            await _openAiEvaluationRepository.CreateEvaluation(promptType, promptSubType, evaluation);



            return evaluation;
        }


        private int? ratePrompt(string prompt, string response)
        {
            if (!_appSettingsProvider.AskForRating)
                return null;

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(prompt.ToString());
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("-----------------------------------");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine(response);

            int ratingNumber = -1;
            while (ratingNumber < 1 || ratingNumber > 10)
            {
                Console.WriteLine("Rate Prompt (1-10):");
                var rating = Console.ReadLine();
                if (rating == null) return null;
                int.TryParse(rating, out ratingNumber);
            }

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            return ratingNumber;
        }


    }
}
