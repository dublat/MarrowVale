using MarrowVale.Business.Entities.Entities;
using MarrowVale.Business.Entities.Entities.Relationships;
using MarrowVale.Common.Constants;
using MarrowVale.Data.Contracts;
using Microsoft.Extensions.Logging;
using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarrowVale.Data.Repositories
{
    public class LocationRepository : BaseRepository<Location>, ILocationRepository
    {
        public LocationRepository(IGraphClient graphClient) : base(graphClient)
        {
        }
        public Room GetBuildingEntrance(string buildingId)
        {
            var path = new PathRelation { IsObstructed = false };
            return RelatedTo<Room, GraphRelationship>(x => x.Id == buildingId, y => true, path).ResultsAsync.Result.FirstOrDefault();
        }

        public Road GetBuildingExit(string buildingId)
        {
            var path = new PathRelation { IsObstructed = false };
            return RelatedTo<Road, GraphRelationship>(x => x.Id == buildingId, y => true, path).ResultsAsync.Result.FirstOrDefault();
        }

        public List<Building> GetNearbyBuildings(Location location)
        {
            var path = new PathRelation { IsObstructed = false };
            return RelatedTo<Building, GraphRelationship>(x => x.Id == location.Id, y => true, path).ResultsAsync.Result.ToList();
        }

        public List<Road> GetNearbyRoads(Location location)
        {
            var path = new PathRelation { IsObstructed = false };
            return RelatedTo<Road, GraphRelationship>(x => x.Id == location.Id, y => true, path).ResultsAsync.Result.ToList();
        }

        public List<Npc> GetNpcsAtLocation(Location location)
        {
            var inside = new GraphRelationship(RelationshipConstants.Inside);
            return RelatedTo<Npc,GraphRelationship>(x => x.Id == location.Id,y=> true, inside, false).ResultsAsync.Result.ToList();
        }

        public List<Room> GetConnectingRooms(Location location)
        {
            var path = new PathRelation { IsObstructed = false };
            return RelatedTo<Room, GraphRelationship>(x => x.Id == location.Id, y => true, path).ResultsAsync.Result.ToList();
        }

        public bool IsPathConnected(Location location, Location location2)
        {
            var path = new PathRelation { IsObstructed = false, PathLength = 2 };
            return RelatedTo<Location, GraphRelationship>(x => x.Id == location.Id, y => y.Id == location2.Id, path).ResultsAsync.Result.Any();
        }

        public bool IsItemAtLocation(Location location, Item item)
        {
            var at = new AtRelation();
            var preQuery = fromCategory("Item");
            return RelatedTo<Item, GraphRelationship>(x => x.Id == location.Id, y => y.Id == item.Id, at, false, preQuery: preQuery).ResultsAsync.Result.Any();
        }
    }
}
