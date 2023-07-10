namespace Tumblr.Bot.Shikaka.Messages.SQLite
{
    internal class AddNewConvoEntityMessage
    {
        public AddNewConvoEntityMessage(
            string botAccountUsername,
            string botUuid,
            string contactUsername,
            string contactUuid,
            string conversationId,
            string sha256Sum)
        {
            BotAccountUsername = botAccountUsername;
            BotUuid = botUuid;
            ContactUsername = contactUsername;
            ContactUuid = contactUuid;
            ConversationId = conversationId;
            Sha256Sum = sha256Sum;
            BotUuid = botUuid;
        }

        public string BotAccountUsername { get; }
        public string BotUuid { get; }
        public string ContactUsername { get; }
        public string ContactUuid { get; }
        public string ConversationId { get; }
        public string Sha256Sum { get; }
    }
}
