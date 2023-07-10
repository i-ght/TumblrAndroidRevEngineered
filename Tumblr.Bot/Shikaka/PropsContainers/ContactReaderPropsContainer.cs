using System.IO;
using Tumblr.Bot.SQLite;
using Waifu.Collections;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class ContactReaderPropsContainer
    {
        public ContactReaderPropsContainer(
            FileStreamWaifu contactStream,
            SQLiteDb sqliteDb)
        {
            ContactStream = contactStream;
            SQLiteDb = sqliteDb;
        }

        public FileStreamWaifu ContactStream { get; }
        public SQLiteDb SQLiteDb { get; }
    }
}
