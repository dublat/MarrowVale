using MarrowVale.Business.Entities.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Entities.Entities
{
    public class MarrowValeMessage
    {
        public string ResultText { get; set; }
        public string ErrorText { get; set; }
        public Command NextCommand { get; set; }

    }
}
