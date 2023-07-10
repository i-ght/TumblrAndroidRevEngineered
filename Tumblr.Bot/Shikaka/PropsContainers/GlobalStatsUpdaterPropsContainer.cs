using Tumblr.Bot.Shikaka.StateContainers;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class GlobalStatsUpdaterPropsContainer
    {
        public GlobalStatsUpdaterPropsContainer(
            GlobalStatsUpdaterStateContainer state)
        {
            State = state;
        }

        public GlobalStatsUpdaterStateContainer State { get; }
    }
}
