namespace Tumblr.Bot.Shikaka.Messages.SQLite
{
    internal class AddGreetBlacklistEntityMessage
    {
        public AddGreetBlacklistEntityMessage(string item)
        {
            Item = item;
        }

        public string Item { get; }
    }
}
