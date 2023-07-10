using System.Threading.Tasks;

namespace Tumblr.Scraper.SQLite
{
    internal class SQLiteDb
    {
        public SQLiteDb(
            KeywordBlacklistTable keywordBlacklistTable)
        {
            KeywordBlacklistTable = keywordBlacklistTable;
        }

        public KeywordBlacklistTable KeywordBlacklistTable { get; }

        public async Task InitializeAsync()
        {
            await KeywordBlacklistTable.InitializeAsync()
                .ConfigureAwait(false);
        }
    }
}
