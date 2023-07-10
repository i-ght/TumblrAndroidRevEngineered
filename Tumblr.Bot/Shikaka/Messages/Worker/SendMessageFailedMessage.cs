using Tumblr.Bot.OutgoingMessages;

namespace Tumblr.Bot.Shikaka.Messages.Worker
{
    internal class SendMessageFailedMessage
    {
        public SendMessageFailedMessage(
            OutgoingMessage outgoingMessage)
        {
            OutgoingMessage = outgoingMessage;
        }

        public OutgoingMessage OutgoingMessage { get; }
    }
}
