﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Common.Evaluator
{
    public class PromptType
    {
        public PromptType()
        {

        }
        public PromptType(string type)
        {
            this.Name = type;
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }
}
