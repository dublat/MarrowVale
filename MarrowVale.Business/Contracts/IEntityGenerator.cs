using MarrowVale.Business.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Business.Contracts
{
    public interface IEntityGenerator
    {
        public void GenerateMap();
        public void GenerateRegions(Map map);
        public void GenerateCities(Region region);
        //public void GenerateCitySectors();
        public Task GenerateBuildings();
        public Task GenerateCharacters();
        public Task DefaultSettings();


    }
}
