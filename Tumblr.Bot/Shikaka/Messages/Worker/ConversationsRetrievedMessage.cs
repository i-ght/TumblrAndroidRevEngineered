using System.Collections.ObjectModel;
using Tumblr.Waifu.JsonObjects;

namespace Tumblr.Bot.Shikaka.Messages.Worker
{
    internal class ConversationsRetrievedMessage
    {
        public ConversationsRetrievedMessage(
            ReadOnlyCollection<TumblrConversation> conversations)
        {
            Conversations = conversations;
        }

        public ReadOnlyCollection<TumblrConversation> Conversations { get; }
    }
}
