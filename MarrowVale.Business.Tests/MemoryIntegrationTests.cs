using System;
using System.Threading.Tasks;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Data.Repositories;
using Neo4jClient;
using Xunit;

namespace MarrowVale.Business.Tests;

public sealed class FixtureFactAttribute : FactAttribute
{
    public FixtureFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("MARROWVALE_CI_FIXTURE") != "1" &&
            Environment.GetEnvironmentVariable("MARROWVALE_DESKTOP_FIXTURE") != "1")
            Skip = "Requires the disposable Neo4j service in the memory CI job.";
    }
}

public class MemoryIntegrationTests
{
    [FixtureFact]
    public async Task CanonicalAndLegacyMemoriesAreReadableByTheirLinkedPlayer()
    {
        if (Environment.GetEnvironmentVariable("MARROWVALE_DESKTOP_FIXTURE") == "1")
        {
            var config = new MarrowVale.Common.Providers.AppSettingsProvider(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance);
            using var desktop = new GraphClient(new Uri(config.Neo4jUrl), config.Neo4jUser, config.Neo4jPassword);
            await desktop.ConnectAsync();
            var database = Environment.GetEnvironmentVariable("MARROWVALE_FIXTURE_DATABASE");
            Assert.Matches("^mvfixture[0-9a-f]{32}$", database);
            desktop.DefaultDatabase = database;
            await VerifyMemory(desktop);
            return;
        }
        using var graph = new GraphClient(new Uri("http://127.0.0.1:17474"), "neo4j", "fixture-password");
        await graph.ConnectAsync();
        await VerifyMemory(graph);
    }

    private static async Task VerifyMemory(IGraphClient graph)
    {
        var npc = new Npc { Id = Guid.NewGuid().ToString() };
        var first = new Player { Id = Guid.NewGuid().ToString() };
        var second = new Player { Id = Guid.NewGuid().ToString() };
        await graph.Cypher.Create("(n:Character {Id:$npc}), (a:Player {Id:$first}), (b:Player {Id:$second})")
            .Create("(n)-[:MEMORY]->(:Conversation {Summary:'old'})-[:MENTIONED]->(b)")
            .WithParams(new { npc = npc.Id, first = first.Id, second = second.Id }).ExecuteWithoutResultsAsync();
        var repo = new NpcRepository(graph);
        Assert.Null(await repo.MemoryofPlayer(npc, first));
        Assert.Equal("old", (await repo.MemoryofPlayer(npc, second)).Summary);
        await repo.CreateMemory(npc, first, "new");
        Assert.Equal("new", (await repo.MemoryofPlayer(npc, first)).Summary);
        Assert.Equal("old", (await repo.MemoryofPlayer(npc, second)).Summary);
        var missing = new Player { Id = Guid.NewGuid().ToString() };
        var marker = Guid.NewGuid().ToString();
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.CreateMemory(npc, missing, marker));
        Assert.Empty(await graph.Cypher.Match("(c:Conversation {Summary:$marker})").WithParam("marker", marker)
            .Return(c => c.As<ConversationNode>()).ResultsAsync);
    }
}
