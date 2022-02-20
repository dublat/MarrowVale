﻿using MarrowVale.Common.Prompts;
using OpenAI_API;
using System.Threading.Tasks;

namespace MarrowVale.Common.Contracts
{
    public interface IOpenAiProvider
    {
        Task<string> BestMatch(string item, string options);
        Task<string> Search(string query, string[] documents);
        Task<string> Complete(string prompt);
        Task<CompletionResult> Complete(CompletionRequest completionRequest, string engineName = "Curie");

    }
}