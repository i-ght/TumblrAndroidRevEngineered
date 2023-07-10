using Tumblr.Waifu;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class ConversationRetrieverPropsContainer
    {
        public ConversationRetrieverPropsContainer(
            TumblrClient client)
        {
            Client = client;
        }

        public TumblrClient Client { get; }
    }
}
