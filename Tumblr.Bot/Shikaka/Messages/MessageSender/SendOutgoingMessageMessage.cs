using Tumblr.Bot.OutgoingMessages;

namespace Tumblr.Bot.Shikaka.Messages.MessageSender
{
    internal class SendOutgoingMessageMessage
    {
        public SendOutgoingMessageMessage(
            OutgoingMessage outgoingMessage)
        {
            OutgoingMessage = outgoingMessage;
        }

        public OutgoingMessage OutgoingMessage { get; }
    }
}
