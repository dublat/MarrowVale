using MarrowVale.Common.Contracts;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Common.Evaluator;
using MarrowVale.Common.Prompts;
using System.Threading.Tasks;

namespace MarrowVale.Business.Contracts
{
    public interface IAiEvaluationService
    {
        Task<Evaluation> CreateEvaluation(AiResult result, string apiName, string prompt, string type, string subtype, OpenAiSettings settings);
    }
}
