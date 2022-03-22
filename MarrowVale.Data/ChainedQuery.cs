using Neo4jClient.Cypher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Data
{
    public class ChainedQuery
    {
        public ICypherFluentQuery Query { get; set; }
        public string Alias { get; set; }
        public Type AliasType { get; set; }

    }
}
