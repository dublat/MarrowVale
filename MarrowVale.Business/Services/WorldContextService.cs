using MarrowVale.Business.Contracts;
using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.Graph;
using MarrowVale.Business.Entities.Enums;
using MarrowVale.Common.Contracts;
using MarrowVale.Data.Contracts;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Services
{
    public class WorldContextService : IWorldContextService
    {
        private Random r = new Random();
        private readonly ITextGenerator _textGenerator;
        private readonly IGraphClient _graphClient;
        private readonly IAiService _aiService;
        private readonly IPlayerRepository _playerRepository;
        private readonly IPromptService _promptService;
        private readonly ILocationRepository _locationRepository;
        private readonly IBuildingRepository _buildingRepository;
        private readonly IRoomRepository _roomRepository;
        private readonly INpcRepository _npcRepository;

        public static List<GraphNode> ContextNodes = new List<GraphNode>();

        public WorldContextService(ITextGenerator textGenerator, IGraphClient graphClient, IAiService aiService, IPlayerRepository playerRepository, IPromptService promptService, ILocationRepository locationRepository,
                                    IBuildingRepository buildingRepository, INpcRepository npcRepository, IRoomRepository roomRepository)
        {
            _textGenerator = textGenerator;
            _graphClient = graphClient;
            _aiService = aiService;
            _playerRepository = playerRepository;
            _promptService = promptService;
            _locationRepository = locationRepository;
            _buildingRepository = buildingRepository;
            _npcRepository = npcRepository;
            _roomRepository = roomRepository;
        }
        public void AttachToContext(string label, string id, object entity)
        {
            throw new NotImplementedException();
        }


        public async Task<T> DirectObjectCommand<T>(string input, string context, GraphNode owner, CommandEnum command) where T : GraphNode
        {

            var prompt = _promptService.GenerateDirectObjectPrompt(input, command);
            var directObject = await _aiService.Complete(prompt.ToString());
            return await GetObjectLabelIdPair<T>(directObject, owner, input);
        }

        public async Task<GraphNode> GetObjectLabelIdPair(string searchTerm, Player player, string input)
        {
            var matches = await _graphClient.Cypher
                .Match("(n)")
                .Where((GraphNode n) => n.Name == searchTerm)
                .With("{Id: n.Id, Name: n.Name, Description: n.Description, Labels: labels(n)} as generalNode")
                .Return(generalNode => generalNode.As<GraphNode>()).ResultsAsync;
            return matches.FirstOrDefault() ?? await ContextSearch(searchTerm, input);
        }

        public async Task<T> GetObjectLabelIdPair<T>(string searchTerm, GraphNode owner, string input) where T : GraphNode
        {
            try
            {
                return (await _graphClient.Cypher
                    .Match("(n)")
                    .Where((T n) => n.Name == searchTerm)
                    .Return(n => n.As<T>())
                    .ResultsAsync).FirstOrDefault();
            }
            catch
            {
                return null;
                //return ContextSearch(searchTerm, input);
            }
        }

        public async Task<string> DescribeConnectedPaths(Player player)
        {
            var currentLocationType = await _playerRepository.PlayerLocationType(player);
            var currentLocation = await _playerRepository.GetPlayerLocation(player);

            var prompt = new StringBuilder();
            prompt.Append("Generate a summary based on the following\n");
            prompt.Append("Name:Windsor Road | Description:A long winding road\n");
            prompt.Append("Name:The Rusty Nail| Description:A small dilapidating bar\n");
            prompt.Append("Name:Giant's Hotel | Description:A Large Beautiful hotel made out of marble\n");
            prompt.Append("Name:Tom's Cafe | Description:A restaurant advertising cheap drinks\n");
            prompt.Append("Summary: Leaving, you are now near Windsor Road, Rusty Nail, an old and run down bar, the Giant's Hotel is nearby.  You can also see Tom's Cafe\n\n");


            var nearbyBuildings = await _graphClient.Cypher
                            .Match("(x:Building)-[:PATH]->(z:Building)")
                            .Where((Building x) => x.Id == currentLocation.Id)
                            .Return(z => z.As<Building>())
                            .ResultsAsync;

            var nearbyRoads = await _graphClient.Cypher
                .Match("(x:Building)-[:PATH]->(z:Road)")
                .Where((Building x) => x.Id == currentLocation.Id)
                .Return(z => z.As<Road>())
                .ResultsAsync;

            foreach (var building in nearbyBuildings)
            {
                prompt.Append($"Name:{building.Name} | Description:{building.Description}\n");
            }
            foreach (var localRoad in nearbyRoads)
            {
                prompt.Append($"Name:{localRoad.Name} | Description:{localRoad.Description}\n");
            }
            prompt.Append("Summary:");
            return await _aiService.Complete(prompt.ToString());
        }


        public Task<GraphNode> ContextSearch(string searchTerm, string conversation)
        {
            var context = new Dictionary<string, GraphNode>();
            foreach (var node in ContextNodes.Where(n => n != null))
                context.TryAdd($"{node.Name}: {node.Description}", node);
            return Search(context, searchTerm);
        }

        public Task<GraphNode> ContextSearch(string searchTerm, GraphNode knowledge)
        {
            var context = new Dictionary<string, GraphNode>();
            var visited = new HashSet<GraphNode>(ReferenceEqualityComparer.Instance);
            void Visit(GraphNode node, string relation, string subject)
            {
                if (node == null) return;
                context.TryAdd($"{subject} {relation} {node.Name}", node);
                if (!visited.Add(node)) return;
                foreach (var edge in node.Relations ?? Enumerable.Empty<BaseRelationship>())
                    if (edge != null) Visit(edge.Node, edge.Relation, node.Name);
            }
            Visit(knowledge, "ARE", "YOU");
            return Search(context, searchTerm);
        }

        private async Task<GraphNode> Search(Dictionary<string, GraphNode> context, string term)
        {
            if (context.Count == 0 || string.IsNullOrWhiteSpace(term)) return null;
            var key = await _aiService.Search(context.Keys, term);
            return key != null && context.TryGetValue(key, out var node) ? node : null;
        }
    }
}
