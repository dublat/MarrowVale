using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Entities.Entities
{
    public class Key : Item
    {
        public Key()
        {
            this.EntityLabel = "Key";
            this.Labels = new List<string>() { EntityLabel };
        }



    }
}
