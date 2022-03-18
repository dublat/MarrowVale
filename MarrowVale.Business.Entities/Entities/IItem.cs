
using MarrowVale.Common.Utilities;
using System.Collections.Generic;

namespace MarrowVale.Business.Entities.Entities
{
    public class Item : GraphNode
    {
        public Item()
        {
            EntityLabel = "Item";
            Labels = new List<string> { EntityLabel };
        }
        public string EnvironmentalDescription { get; set; }
        public bool IsVisible { get; set; }
        public int BaseWorth { get; set; }
        public virtual string GetShortDescription()
        {
            return IsVisible ? $"{Name} - {Description} - {CurrencyUtility.StandardizeCurrency(BaseWorth)}" : "";
        }
        public virtual string GetDescription()
        {
            return "";
        }

        private string convertCurrency(int currency)
        {
            return $"{currency} bronze";  
        }
    }
}
