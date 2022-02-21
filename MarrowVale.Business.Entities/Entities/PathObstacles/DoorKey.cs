using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Entities.Entities.PathObstacles
{
    public class DoorKey : Key
    {
        public DoorKey()
        {
            this.EntityLabel = "DoorKey";
            this.Labels = new List<string>() { EntityLabel };
        }

        public IEnumerable<Door> UnlocksDoors { get; set; }


    }
}
