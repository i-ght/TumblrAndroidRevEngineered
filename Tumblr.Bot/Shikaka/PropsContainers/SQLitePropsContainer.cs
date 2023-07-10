using Tumblr.Bot.SQLite;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class SQLitePropsContainer
    {
        public SQLitePropsContainer(SQLiteDb sqLiteDb)
        {
            SQLiteDb = sqLiteDb;
        }

        public SQLiteDb SQLiteDb { get; }
    }
}
