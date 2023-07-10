using System;

namespace Tumblr.RecentActivityChecker.Work
{
    internal class TumblrContact
    {
        public TumblrContact(
            string username,
            string uuid,
            TimeSpan timeSinceLastActivity)
        {
            Username = username;
            Uuid = uuid;
            TimeSinceLastActivity = timeSinceLastActivity;
        }

        public TumblrContact(
            string username,
            string uuid) : 
            this(
                username, 
                uuid,
                TimeSpan.Zero
            )
        {
        }

        public string Username { get; }
        public string Uuid { get; }
        public TimeSpan TimeSinceLastActivity { get; set; }

        public override string ToString()
        {
            return $"{Username}|{Uuid}";
        }

        public static bool TryParse(string input, out TumblrContact contact)
        {
            contact = null;

            if (string.IsNullOrWhiteSpace(input) ||
                !input.Contains("|"))
            {
                return false;
            }

            var split = input.Split('|');
            if (split.Length != 2)
                return false;

            var username = split[0];
            var uuid = split[1];

            contact = new TumblrContact(username, uuid);
            return true;
        }
    }
}
