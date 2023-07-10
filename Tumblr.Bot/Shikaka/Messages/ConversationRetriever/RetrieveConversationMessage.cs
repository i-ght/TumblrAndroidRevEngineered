namespace Tumblr.Bot.Shikaka.Messages.ConversationRetriever
{
    internal class RetrieveConversationMessage
    {
        public RetrieveConversationMessage(
            string conversatioId,
            string withUuid)
        {
            ConversatioId = conversatioId;
            WithUuid = withUuid;
        }

        public string ConversatioId { get; }
        public string WithUuid { get; }
    }
}
