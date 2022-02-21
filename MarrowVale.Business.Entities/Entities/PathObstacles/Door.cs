using MarrowVale.Business.Entities.Entities.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Entities.Entities.PathObstacles
{
    public class Door : PathObstacle
    {
        public Door()
        {
            this.EntityLabel = "Door";
            this.Labels = new List<string>() { EntityLabel };
        }
        public bool IsClosed { get; set; }
        public bool IsLocked { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Health { get; set; }
        public OntologyNode Material { get; set; }

    }
}
