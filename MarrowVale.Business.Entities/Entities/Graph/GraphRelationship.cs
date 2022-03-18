using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MarrowVale.Business.Entities.Entities
{ 
    public class GraphRelationship
    {
        public GraphRelationship()
        {
            Labels = new List<string>();
            Id = Guid.NewGuid().ToString();
        }
        public GraphRelationship(string name, int pathLength = 1, bool? isDirectedOut = null) : this()
        {
            PrimaryLabel = name;
            PathLength = pathLength;
            IsDirectedOut = isDirectedOut;
        }

        public string Id { get; set; }
        [JsonIgnore]
        public bool? IsDirectedOut { get; set; }
        [JsonIgnore]
        public string Alias { get; set; }
        [JsonIgnore]
        public string PrimaryLabel { get; set; }
        [JsonIgnore]
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

            if (IsDirectedOut == true)
            {
                right = "->";
            }
            else if (IsDirectedOut == false)
            {
                left = "<-";
            }

            if (PathLength == 1)
                return $"{left}[{Alias}:{PrimaryLabel} {FormatProperties()}]{right}";

            return $"{left}[{Alias}:{PrimaryLabel}* ..{PathLength} {FormatProperties()}]{right}";
        }

        //Set this method in children relationships
        public virtual string FormatProperties()
        {
            return "";
        }

    }
}
