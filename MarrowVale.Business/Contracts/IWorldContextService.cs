using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Contracts
{
    public interface IWorldContextService
    {
        public Task<GraphNode> GetObjectLabelIdPair(string searchObject, Player owner, string input);
        public Task<T> DirectObjectCommand<T>(string input, string context, GraphNode owner, CommandEnum command) where T : GraphNode;
        public void AttachToContext(string label, string id, Object entity);
        public Task<string> DescribeConnectedPaths(Player player);
        public Task<GraphNode> ContextSearch(string searchTerm, string conversation);
        public Task<GraphNode> ContextSearch(string searchTerm, GraphNode characterKnowledge);
    }
}
