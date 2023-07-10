using Tumblr.Waifu;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class SessionCheckerPropsContainer
    {
        public SessionCheckerPropsContainer(
            TumblrClient client)
        {
            Client = client;
        }

        public TumblrClient Client { get; }
    }
}
