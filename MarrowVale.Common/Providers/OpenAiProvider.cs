using MarrowVale.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MarrowVale.Common.Providers;

public class OpenAiProvider : IOpenAiProvider
{
    private readonly HttpClient client;
    private readonly IAppSettingsProvider settings;
    private readonly CancellationToken cancellation;

    public OpenAiProvider(HttpClient client, IAppSettingsProvider settings, CancellationToken cancellation = default)
    {
        this.client = client;
        this.settings = settings;
        this.cancellation = cancellation;
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private async Task<JsonDocument> Post(string endpoint, object body)
    {
        if (string.IsNullOrWhiteSpace(settings.OpenAiKey))
            throw new InvalidOperationException("Configure OpenAiKey before using AI.");
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/" + endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.OpenAiKey);
        request.Content = JsonContent.Create(body, options: Json);
        using var response = await client.SendAsync(request, cancellation);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellation));
    }

    private static AiResult Result(JsonElement root, string text, Stopwatch timer) =>
        new(text, root.GetProperty("model").GetString(),
            root.TryGetProperty("usage", out var usage) && usage.TryGetProperty("total_tokens", out var tokens)
                ? tokens.GetInt32() : null, timer.Elapsed);

    public async Task<AiResult> Complete(IReadOnlyList<ChatTurn> messages, ChatOptions options)
    {
        var timer = Stopwatch.StartNew();
        using var response = await Post("chat/completions", new
        {
            model = options.Model, messages, max_completion_tokens = options.MaxTokens,
            temperature = options.Temperature, top_p = options.TopP, presence_penalty = options.PresencePenalty,
            frequency_penalty = options.FrequencyPenalty, stop = options.Stop,
            logprobs = options.Logprobs.HasValue ? true : (bool?)null, top_logprobs = options.Logprobs
        });
        var root = response.RootElement;
        var answer = root.GetProperty("choices").EnumerateArray().FirstOrDefault();
        if (answer.ValueKind != JsonValueKind.Object || !answer.TryGetProperty("message", out var message)
            || !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(content.GetString()))
            throw new InvalidDataException("The chat response contained no text.");
        return Result(root, content.GetString(), timer);
    }

    public async Task<AiResult> SemanticSearch(IEnumerable<string> documents, string query)
    {
        var candidates = documents?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToArray();
        if (string.IsNullOrWhiteSpace(query) || candidates == null || candidates.Length == 0)
            return null;
        var timer = Stopwatch.StartNew();
        using var response = await Post("embeddings", new { model = settings.EmbeddingModel, input = candidates.Prepend(query).ToArray() });
        var root = response.RootElement;
        var vectors = root.GetProperty("data").EnumerateArray().OrderBy(e => e.GetProperty("index").GetInt32())
            .Select(e => e.GetProperty("embedding").EnumerateArray().Select(n => n.GetDouble()).ToArray()).ToArray();
        if (vectors.Length != candidates.Length + 1 || vectors.Any(v => v.Length == 0 || v.Length != vectors[0].Length))
            throw new InvalidDataException("The embedding response had unexpected dimensions.");
        var best = -1;
        var score = double.NegativeInfinity;
        for (var i = 0; i < candidates.Length; i++)
        {
            var v = vectors[i + 1];
            var similarity = v.Zip(vectors[0], (x,y) => x*y).Sum() /
                Math.Sqrt(v.Sum(x => x*x) * vectors[0].Sum(x => x*x));
            if (similarity > score) { best = i; score = similarity; }
        }
        return Result(root, best < 0 ? null : candidates[best], timer);
    }
}
