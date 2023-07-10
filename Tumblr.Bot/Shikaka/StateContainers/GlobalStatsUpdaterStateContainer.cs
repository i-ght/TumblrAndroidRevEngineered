using Tumblr.Bot.UserInterface;

namespace Tumblr.Bot.Shikaka.StateContainers
{
    internal class GlobalStatsUpdaterStateContainer
    {
        public GlobalStatsUpdaterStateContainer(
            GlobalStats globalStats)
        {
            GlobalStats = globalStats;
        }

        public GlobalStats GlobalStats { get; }
    }
}
