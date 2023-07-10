namespace Tumblr.Bot.Shikaka.Messages.Worker
{
    internal class SessionOkMessage
    {
        public SessionOkMessage(int unreadMessagesCount)
        {
            UnreadMessagesCount = unreadMessagesCount;
        }

        public int UnreadMessagesCount { get; }
    }
}
