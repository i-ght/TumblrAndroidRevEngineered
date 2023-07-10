using System.Collections.Generic;
using Tumblr.Bot.Enums;
using Tumblr.Bot.OutgoingMessages;

namespace Tumblr.Bot.Shikaka.StateContainers
{
    internal class MessageSenderStateContainer
    {
        public MessageSenderStateContainer()
        {
            PendingLinkMessages = new Queue<OutgoingMessage>();
            PendingReplyMessages = new Queue<OutgoingMessage>();
            PendingGreetMessages = new Queue<OutgoingMessage>();
            ScheduledMessages = new List<PendingCancelableOutgoingMessage>();
        }

        public Queue<OutgoingMessage> PendingLinkMessages { get; }
        public Queue<OutgoingMessage> PendingReplyMessages { get; }
        public Queue<OutgoingMessage> PendingGreetMessages { get; }
        public List<PendingCancelableOutgoingMessage> ScheduledMessages { get; }

        public MessageSenderActorBehaviorState BehaviorState { get; set; }
        public int PendingMessages { get; set; }
    }
}
