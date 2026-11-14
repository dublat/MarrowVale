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
        public async Task<Room> GetBuildingEntrance(string buildingId)
        {
            var path = new PathRelation { IsObstructed = false, IsDirectedOut = true };
            return (await RelatedTo<Room, GraphRelationship>(x => x.Id == buildingId, y => true, path).ResultsAsync).FirstOrDefault();
        }

        public async Task<Road> GetBuildingExit(string buildingId)
        {
            var path = new PathRelation { IsObstructed = false, IsDirectedOut = true };
            return (await RelatedTo<Road, GraphRelationship>(x => x.Id == buildingId, y => true, path).ResultsAsync).FirstOrDefault();
        }

        public async Task<List<Building>> GetNearbyBuildings(Location location)
        {
            var path = new PathRelation { IsObstructed = false, IsDirectedOut = true };
            return (await RelatedTo<Building, GraphRelationship>(x => x.Id == location.Id, y => true, path).ResultsAsync).ToList();
        }

        public async Task<List<Road>> GetNearbyRoads(Location location)
        {
            var path = new PathRelation { IsObstructed = false };
            return (await RelatedTo<Road, GraphRelationship>(x => x.Id == location.Id, y => true, path).ResultsAsync).ToList();
        }

        public async Task<List<Npc>> GetNpcsAtLocation(Location location)
        {
            var inside = new GraphRelationship(RelationshipConstants.Inside, isDirectedOut: false);
            return (await RelatedTo<Npc,GraphRelationship>(x => x.Id == location.Id,y=> true, inside).ResultsAsync).ToList();
        }

        public async Task<List<Room>> GetConnectingRooms(Location location)
        {
            var path = new PathRelation { IsObstructed = false, IsDirectedOut = true };
            return (await RelatedTo<Room, GraphRelationship>(x => x.Id == location.Id, y => true, path).ResultsAsync).ToList();
        }

        public async Task<bool> IsPathConnected(Location location, Location location2)
        {
            var path = new PathRelation { IsObstructed = false, PathLength = 2, IsDirectedOut = false };
            return (await RelatedTo<Location, GraphRelationship>(x => x.Id == location.Id, y => y.Id == location2.Id, path).ResultsAsync).Any();
        }

        public async Task<bool> IsItemAtLocation(Location location, Item item)
        {
            var at = new AtRelation { IsDirectedOut = false };
            var preQuery = fromCategory("Item");
            return (await RelatedTo<Item, GraphRelationship>(x => x.Id == location.Id, y => y.Id == item.Id, at, preQuery: preQuery).ResultsAsync).Any();
        }
    }
}
