using Tumblr.Bot.OutgoingMessages;

namespace Tumblr.Bot.Shikaka.Messages.MessageSender
{
    internal class ScheduleOutgoingMessageMessage
    {
        public ScheduleOutgoingMessageMessage(
            OutgoingMessage outgoingMessage)
        {
            OutgoingMessage = outgoingMessage;
        }

        public OutgoingMessage OutgoingMessage { get; }
    }
}
