using OpenAI_API.Completions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarrowVale.Common.Contracts
{
    public interface IOpenAiProvider
    {
        Task<string> BestMatch(string item, string options);
        Task<string> SemanticSearch(IEnumerable<string> documents, string searchTermtring);
        Task<CompletionResult> Complete(CompletionRequest completionRequest);

    }
}
