using MarrowVale.Common.Prompts;
using MarrowVale.Common.Contracts;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarrowVale.Business.Contracts
{
    public interface IAiService
    {
        Task<string> Search(IEnumerable<string> documents, string searchTerm);
        Task<string> Complete(string prompt);
        Task<string> Complete(StandardPrompt prompt);
        Task<(IReadOnlyList<ChatTurn> conversation, string response)> SendChat(string message, string systemMessage, IReadOnlyList<ChatTurn> conversation = null, string subtype = "Chat");

    }
}
