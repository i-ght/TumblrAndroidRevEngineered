using Tumblr.Bot.Shikaka.StateContainers;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class ConnectionPermintGranterPropsContainer
    {
        public ConnectionPermintGranterPropsContainer(
            int maxPermits,
            ConnectionPermitGranterStateContainer state)
        {
            MaxPermits = maxPermits;
            State = state;
        }
        public int MaxPermits { get; }
        public ConnectionPermitGranterStateContainer State { get; }
    }
}
