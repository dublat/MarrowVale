using MarrowVale.Common.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Entities.Entities.Relationships
{
    public class AtRelation : GraphRelationship
    {
        public AtRelation() : base(RelationshipConstants.At) {}

        public bool IsVisible { get; set; }

        public override string ToString()
        {
            return $"{PrimaryLabel}* ..{PathLength} {{IsVisible: {IsVisible}}}";
        }
    }
}
