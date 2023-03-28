using MarrowVale.Common.Prompts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarrowVale.Business.Contracts
{
    public interface IAiService
    {
        Task<string> BestMatch(string item, string options);
        Task<string> Search(IEnumerable<string> documents, string searchTerm);
        Task<string> Complete(string prompt);
        Task<string> Complete(StandardPrompt prompt);

    }
}
