namespace Tumblr.Bot.Shikaka.Messages.GreetSessionHandler
{
    internal class ContactMessage
    {
        public ContactMessage()
        {
            Username = string.Empty;
            Uuid = string.Empty;
        }

        public ContactMessage(
            string username,
            string uuid)
        {
            Username = username;
            Uuid = uuid;
        }

        public string Username { get; }
        public string Uuid { get; }

        public override string ToString()
        {
            return $"{Username}|{Uuid}";
        }
    }
}
