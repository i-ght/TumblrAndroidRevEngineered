using Tumblr.Bot.OutgoingMessages;

namespace Tumblr.Bot.Shikaka.Messages.Worker
{
    internal class MessageSentSuccessfullyMessage
    {
        public MessageSentSuccessfullyMessage(
            OutgoingMessage outgoingMessage)
        {
            OutgoingMessage = outgoingMessage;
        }

        public OutgoingMessage OutgoingMessage { get; }
    }
}
