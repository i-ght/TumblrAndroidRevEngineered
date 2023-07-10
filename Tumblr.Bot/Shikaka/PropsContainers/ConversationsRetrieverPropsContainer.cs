using Tumblr.Bot.Shikaka.StateContainers;
using Tumblr.Waifu;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class ConversationsRetrieverPropsContainer
    {
        public ConversationsRetrieverPropsContainer(
            TumblrClient client,
            ConversationsRetrieverStateContainer state)
        {
            Client = client;
            State = state;
        }

        public TumblrClient Client { get; }
        public ConversationsRetrieverStateContainer State { get; }
    }
}
