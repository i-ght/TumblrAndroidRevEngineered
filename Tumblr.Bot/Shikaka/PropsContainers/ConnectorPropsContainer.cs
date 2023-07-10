using Tumblr.Waifu;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class ConnectorPropsContainer
    {
        public ConnectorPropsContainer(
            TumblrClient client)
        {
            Client = client;
        }

        public TumblrClient Client { get; }
    }
}
