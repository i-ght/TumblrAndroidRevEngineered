namespace Tumblr.Creator.SQLite
{
    internal class SQLiteDb
    {
        public SQLiteDb(
            EmailBlacklistAccessProvider emailBlacklist)
        {
            EmailBlacklist = emailBlacklist;
        }

        public EmailBlacklistAccessProvider EmailBlacklist { get; }
    }
}
