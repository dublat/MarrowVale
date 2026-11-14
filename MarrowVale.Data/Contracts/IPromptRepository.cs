using MarrowVale.Common.Evaluator;
using MarrowVale.Business.Entities.Entities;
using System.Threading.Tasks;

namespace MarrowVale.Data.Contracts
{
    public interface IPromptRepository : IBaseRepository<PromptType>
    {
        Task CreateDefaultPromptSetting(string promptType, string subPromptType, OpenAiSettings request);
        Task<OpenAiSettings> GetDefaultPromptSetting(string promptType, string subPromptType, OpenAiSettings request);
        Task UpdateDefaultPromptSetting(Player player);
        Task EnsurePromptCreated(PromptType promptType, PromptSubType promptSubType);
    }
}
