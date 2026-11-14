using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MarrowVale.Common.Contracts;
using MarrowVale.Common.Providers;
using Moq;
using Xunit;

namespace MarrowVale.Business.Tests;

public class ProviderTests
{
    private sealed class Handler : HttpMessageHandler
    {
        private readonly string json;

        public Handler(string json)
        {
            this.json = json;
        }

        public int Calls { get; private set; }
        public string Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Calls++;
            Body = await request.Content.ReadAsStringAsync(token);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        }
    }

    private static IAppSettingsProvider Settings => Mock.Of<IAppSettingsProvider>(s =>
        s.OpenAiKey == "fixture" && s.EmbeddingModel == "embedding-fixture");

    [Fact]
    public async Task SearchRanksBatchByCosineAndIgnoresDuplicateDocuments()
    {
        var handler = new Handler("{\"model\":\"emb\",\"usage\":{\"total_tokens\":11},\"data\":[{\"index\":2,\"embedding\":[0,3]},{\"index\":0,\"embedding\":[0,1]},{\"index\":1,\"embedding\":[2,0]}]}");
        using var client = new HttpClient(handler);
        IOpenAiProvider provider = new OpenAiProvider(client, Settings);
        Assert.Null(await provider.SemanticSearch(Array.Empty<string>(), "shop"));
        Assert.Equal(0, handler.Calls);
        var result = await provider.SemanticSearch(new[] { "road", "store", "road", " " }, "shop");
        Assert.Equal("store", result.Text);
        Assert.Equal(11, result.TotalTokens);
        using var body = JsonDocument.Parse(handler.Body);
        Assert.Equal(new[] { "shop", "road", "store" }, body.RootElement.GetProperty("input").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task ChatSerializesConfiguredSettingsAndReadsUsage()
    {
        var handler = new Handler("{\"model\":\"fixture-version\",\"choices\":[{\"message\":{\"content\":\"answer\"}}],\"usage\":{\"total_tokens\":17}}");
        using var client = new HttpClient(handler);
        var provider = new OpenAiProvider(client, Settings);
        var options = new ChatOptions("fixture", 50, .2, .9, .1, .3, new[] { "END" }, 2);
        var result = await provider.Complete(new[] { new ChatTurn("user", "question") }, options);
        Assert.Equal(17, result.TotalTokens);
        Assert.Equal("fixture-version", result.Model);
        using var body = JsonDocument.Parse(handler.Body);
        var root = body.RootElement;
        Assert.Equal("fixture", root.GetProperty("model").GetString());
        Assert.Equal(50, root.GetProperty("max_completion_tokens").GetInt32());
        Assert.Equal(.9, root.GetProperty("top_p").GetDouble());
        Assert.Equal("END", root.GetProperty("stop")[0].GetString());
        Assert.Equal("user", root.GetProperty("messages")[0].GetProperty("role").GetString());
    }

    [Fact]
    public async Task EmptyChoicesAreAnObservedFailure()
    {
        using var client = new HttpClient(new Handler("{\"model\":\"fixture\",\"choices\":[]}"));
        var provider = new OpenAiProvider(client, Settings);
        await Assert.ThrowsAsync<InvalidDataException>(() => provider.Complete(Array.Empty<ChatTurn>(),
            new ChatOptions("fixture", null, null, null, null, null, null, null)));
    }
}
