namespace Tumblr.Bot.Shikaka.Messages.GreetSessionHandler
{
    internal class TellMessageSenderSendMessageMessage
    {
        public TellMessageSenderSendMessageMessage(
            string contactUsername,
            string contactUuid,
            string messageBody)
        {
            ContactUuid = contactUuid;
            MessageBody = messageBody;
            ContactUsername = contactUsername;
        }

        public string ContactUsername { get; }
        public string ContactUuid { get; }
        public string MessageBody { get; }
    }
}
