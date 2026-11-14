using System.Collections.Generic;

namespace MarrowVale.Business.Entities.Entities
{
    public class ConversationNode : GraphNode
    {
        public ConversationNode()
        {
            this.EntityLabel = "Conversation";
            this.Labels = new List<string>() { EntityLabel, "Memory" };
        }
        public string Summary { get; set; }

    }
}
