using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Entities.Entities
{
    public class PathObstacle : GraphNode
    {
        public PathObstacle()
        {
            this.EntityLabel = "PathObstacle";
            this.Labels = new List<string>() { EntityLabel };
        }
        public bool IsRestricted { get; set; }
        public bool IsBlocked { get; set; }

    }
}
