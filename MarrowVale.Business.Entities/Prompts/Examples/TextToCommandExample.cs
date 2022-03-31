using MarrowVale.Common.Prompts.Examples;
using System.Collections.Generic;

namespace MarrowVale.Business.Entities.Prompts.Examples
{
    public class TextToCommandExample : BaseExample
    {
        public string Command { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
    }
}
