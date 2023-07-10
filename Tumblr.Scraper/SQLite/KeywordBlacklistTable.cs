using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Waifu.SQLite;

namespace Tumblr.Scraper.SQLite
{
    internal class KeywordBlacklistTable :
        SQLiteDbTableAccessProvider<BlacklistItemEntity, int?>
    {
        public KeywordBlacklistTable(
            string dbFileName,
            string tableName,
            string primaryKeyColumnName,
            IReadOnlyDictionary<string, string> columns)
        : base(dbFileName, tableName, primaryKeyColumnName, columns)
        {
        }

        public async Task<bool> ContainsItemAsync(string item)
        {
            var obj = new { Item = item };
            var query = $"SELECT 1 FROM \"{TableName}\" WHERE \"Item\" = @Item LIMIT 1;";
            var result = await Connection.QueryFirstOrDefaultAsync<BlacklistItemEntity>(query, obj)
                .ConfigureAwait(false);
            return result != null;
        }
    }
}