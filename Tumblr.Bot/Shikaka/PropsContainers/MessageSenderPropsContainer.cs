using Tumblr.Bot.Shikaka.StateContainers;
using Tumblr.Waifu;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class MessageSenderPropsContainer
    {
        public MessageSenderPropsContainer(
            int maxConcurrentReplies,
            TumblrClient client,
            MessageSenderStateContainer state)
        {
            MaxConcurrentReplies = maxConcurrentReplies;
            Client = client;
            State = state;
        }

        public int MaxConcurrentReplies { get; }
        public TumblrClient Client { get; }
        public MessageSenderStateContainer State { get; }
    }
}
