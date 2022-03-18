using Newtonsoft.Json;
using System.Collections.Generic;

namespace MarrowVale.Business.Entities.Entities
{ 
    public class GraphRelationship
    {
        public GraphRelationship()
        {
            Labels = new List<string>();            
        }
        public GraphRelationship(string name, int pathLength = 1) : this()
        {
            PrimaryLabel = name;
            PathLength = pathLength;
        }
        public bool? IsDirectionOut { get; set; }
        public string Alias { get; set; }
        public string PrimaryLabel { get; set; }
        public int PathLength { get; set; }
        [JsonIgnore]
        public IEnumerable<string> Labels { get; set; }


        public virtual string FormattedLabels()
        {
            return string.Join(':', Labels);
        }

        public override string ToString()
        {
            var left = "-";
            var right = "-";

            if (IsDirectionOut == true)
            {
                right = "->";
            }
            else if (IsDirectionOut == false)
            {
                left = "<-";
            }

            if (PathLength == 1)
                return PrimaryLabel;

            return $"{left}[{Alias}:{PrimaryLabel}* ..{PathLength}]{right}";
        }

        //public abstract string FormatProperties();

    }
}
