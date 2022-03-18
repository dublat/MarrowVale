using MarrowVale.Business.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Data
{
    public static class AliasManager
    {
        private static bool[] _activeAliases = new bool[64];
        private static Dictionary<GraphNode, int> _nodeLookup = new Dictionary<GraphNode, int>();
        private static Dictionary<GraphRelationship, int> _relationshipLookup = new Dictionary<GraphRelationship, int>();

        public static string CreateAlias(GraphNode node)
        {
            for (int i = 0; i < _activeAliases.Length; i++)
            {
                if (_activeAliases[i] == false)
                {
                    _nodeLookup.Add(node, i);
                    _activeAliases[i] = true;
                    return generateAlias(i);
                }
            }
            throw new Exception("Not enough space in active alias array");
        }

        public static string CreateAlias(GraphRelationship relationship)
        {
            for (int i = 0; i < _activeAliases.Length; i++)
            {
                if (_activeAliases[i] == false)
                {
                    _relationshipLookup.Add(relationship, i);
                    _activeAliases[i] = true;
                    return generateAlias(i);
                }
            }
            throw new Exception("Not enough space in active alias array");
        }

        public static void DisposeAlias(GraphNode node)
        {
            _activeAliases[_nodeLookup[node]] = false;
            _nodeLookup.Remove(node);
        }

        public static void DisposeAlias(GraphRelationship relationship)
        {
            _activeAliases[_relationshipLookup[relationship]] = false;
            _relationshipLookup.Remove(relationship);
        }



        public static void DisposeAllAliases()
        {
            _activeAliases = new bool[64];
            _nodeLookup = new Dictionary<GraphNode, int>();
            _relationshipLookup = new Dictionary<GraphRelationship, int>();
        }

        private static string generateAlias(int position)
        {
            if (position == 0)
                return "A";

            var alias = new StringBuilder();
            var charAmount = 26;

            while (position != 0)
            {
                var remainder = (position) % charAmount;
                alias.Append((char)remainder);
                position = position / charAmount;
            }
            return alias.ToString().ToUpper();
        }


    }
}
