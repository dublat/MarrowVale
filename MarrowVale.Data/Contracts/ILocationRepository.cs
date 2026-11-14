using MarrowVale.Business.Entities.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MarrowVale.Data.Contracts
{
    public interface ILocationRepository : IBaseRepository<Location>
    {
        Task<Room> GetBuildingEntrance(string buildingId);
        Task<Road> GetBuildingExit(string buildingId);
        Task<List<Building>> GetNearbyBuildings(Location location);
        Task<List<Road>> GetNearbyRoads(Location location);
        Task<List<Npc>> GetNpcsAtLocation(Location location);
        Task<List<Room>> GetConnectingRooms(Location location);
        Task<bool> IsPathConnected(Location location, Location location2);
        Task<bool> IsItemAtLocation(Location location, Item item);
    }
}
