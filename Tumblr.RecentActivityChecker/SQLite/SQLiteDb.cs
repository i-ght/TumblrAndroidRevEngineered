namespace Tumblr.RecentActivityChecker.SQLite
{
    internal class SQLiteDb
    {
        public SQLiteDb(
            ContactBlacklistSQLiteTable contactBlacklist)
        {
            ContactBlacklist = contactBlacklist;
        }

        public ContactBlacklistSQLiteTable ContactBlacklist { get; }
    }
}
