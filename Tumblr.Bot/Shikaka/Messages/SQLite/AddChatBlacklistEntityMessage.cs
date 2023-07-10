namespace Tumblr.Bot.Shikaka.Messages.SQLite
{
    internal class AddChatBlacklistEntityMessage
    {
        public AddChatBlacklistEntityMessage(string item)
        {
            Item = item;
        }

        public string Item { get; }
    }
}
