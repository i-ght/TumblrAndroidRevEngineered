namespace Tumblr.Bot.Shikaka.StateContainers
{
    internal class ConnectionPermitGranterStateContainer
    {
        public ConnectionPermitGranterStateContainer(
            int connectionSlotsAvailable)
        {
            ConnectionSlotsAvailable = connectionSlotsAvailable;
        }

        public int ConnectionSlotsAvailable { get; set; }
    }
}
