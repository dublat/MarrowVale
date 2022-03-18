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

        //Id of Relationship or Node is the key of these dictionaries
        private static Dictionary<string, int> _nodeLookup = new Dictionary<string, int>();
        private static Dictionary<string, int> _relationshipLookup = new Dictionary<string, int>();



        public static void CreateAlias(params GraphNode[] nodes)
        {
            foreach(var node in nodes ?? Enumerable.Empty<GraphNode>())
            {
                CreateAlias(node);
            }
        }

        public static void CreateAlias(params GraphRelationship[] relationships)
        {
            foreach (var relationship in relationships ?? Enumerable.Empty<GraphRelationship>())
            {
                CreateAlias(relationship);
            }
        }

        public static void DisposeAlias(params GraphNode[] nodes)
        {
            foreach (var node in nodes ?? Enumerable.Empty<GraphNode>())
            {
                DisposeAlias(node);
            }
        }

        public static void DisposeAlias(params GraphRelationship[] relationships)
        {
            foreach (var relationship in relationships ?? Enumerable.Empty<GraphRelationship>())
            {
                DisposeAlias(relationship);
            }
        }


        public static void CreateAlias(GraphNode node)
        {
            for (int i = 0; i < _activeAliases.Length; i++)
            {
                if (_activeAliases[i] == false)
                {
                    _nodeLookup.Add(node.Id, i);
                    _activeAliases[i] = true;
                    node.Alias = generateAlias(i);
                    return;
                }
            }
            throw new Exception("Not enough space in active alias array");
        }

        public static void CreateAlias(GraphRelationship relationship)
        {
            for (int i = 0; i < _activeAliases.Length; i++)
            {
                if (_activeAliases[i] == false)
                {
                    _relationshipLookup.Add(relationship.Id, i);
                    _activeAliases[i] = true;
                    relationship.Alias = generateAlias(i);
                    return;
                }
            }
            throw new Exception("Not enough space in active alias array");
        }

        public static void DisposeAlias(GraphNode node)
        {
            _activeAliases[_nodeLookup[node.Id]] = false;
            _nodeLookup.Remove(node.Id);
        }

        public static void DisposeAlias(GraphRelationship relationship)
        {
            _activeAliases[_relationshipLookup[relationship.Id]] = false;
            _relationshipLookup.Remove(relationship.Id);
        }



        public static void DisposeAllAliases()
        {
            _activeAliases = new bool[64];
            _nodeLookup = new Dictionary<string, int>();
            _relationshipLookup = new Dictionary<string, int>();
        }

        private static string generateAlias(int position)
        {
            if (position == 0)
                return "A";

            var alias = new StringBuilder();
            var charAmount = 26;
            var asciiConversion = 65;

            while (position != 0)
            {
                var remainder = (position) % charAmount;
                alias.Append((char)(remainder + asciiConversion));
                position = position / charAmount;
            }
            return alias.ToString().ToUpper();
        }


    }
}
