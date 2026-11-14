using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarrowVale.Common.Contracts;

public record ChatTurn(string Role, string Content);
public record AiResult(string Text, string Model, int? TotalTokens, TimeSpan Duration);
public record ChatOptions(string Model, int? MaxTokens, double? Temperature, double? TopP,
    double? PresencePenalty, double? FrequencyPenalty, string[] Stop, int? Logprobs);

public interface IOpenAiProvider
{
    Task<AiResult> Complete(IReadOnlyList<ChatTurn> messages, ChatOptions options);
    Task<AiResult> SemanticSearch(IEnumerable<string> documents, string query);
}
