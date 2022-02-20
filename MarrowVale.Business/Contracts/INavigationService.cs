using MarrowVale.Business.Entities.Commands;
using MarrowVale.Business.Entities.Entities;


namespace MarrowVale.Business.Contracts
{
    public interface INavigationService
    {
        public bool IsPathValid(Location playerLocation, Location destination);
        public MarrowValeMessage Enter(Command command, Player player);
        public MarrowValeMessage Exit(Command command, Player player);
        public string TraversePath(Player player, Location location);
        public string ExitBuilding(Player player);
        public string ClimbUp(Player player, Location location);
        public string ClimbDown(Player player, Location location);
        public string FastTravel(Player player, Location location);
        public MarrowValeMessage CurrentLocationDescription(Player player);
    }
}
