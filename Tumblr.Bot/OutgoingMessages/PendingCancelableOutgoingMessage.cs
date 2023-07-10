using Akka.Actor;
using Tumblr.Bot.Shikaka.Messages.MessageSender;

namespace Tumblr.Bot.OutgoingMessages
{
    internal class PendingCancelableOutgoingMessage
    {
        public PendingCancelableOutgoingMessage(
            ICancelable cancellationApparatus,
            SendOutgoingMessageMessage message)
        {
            CancellationApparatus = cancellationApparatus;
            Message = message;
        }

        public ICancelable CancellationApparatus { get; }
        public SendOutgoingMessageMessage Message { get; }
    }
}
