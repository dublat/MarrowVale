using MarrowVale.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Entities.Entities.Relationships
{
    public class PathRelation : GraphRelationship
    {
        public PathRelation() : base(RelationshipConstants.Path) {}

        public bool? IsObstructed { get; set; }

        public override string FormatProperties()
        {
            if (IsObstructed is not null)
                return $"{{IsObstructed: {IsObstructed}}}";
            return "";
        }
    }
}
