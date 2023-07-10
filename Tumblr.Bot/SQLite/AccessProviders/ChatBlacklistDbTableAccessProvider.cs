using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Tumblr.Bot.SQLite.Entities;
using Waifu.SQLite;
using Waifu.SQLite.Helpers;

namespace Tumblr.Bot.SQLite.AccessProviders
{
    internal class ChatBlacklistDbTableAccessProvider : 
        SQLiteDbTableAccessProvider<BlacklistItemEntity, int?>
    {
        public ChatBlacklistDbTableAccessProvider(
            string dbFileName,
            string tableName,
            string primaryKeyColumnName,
            IReadOnlyDictionary<string, string> columns) 
            : base(dbFileName, tableName, primaryKeyColumnName, columns)
        {
        }

        public async Task<bool> ContainsItemAsync(string item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var obj = new { Item = item };
            //var query = $"SELECT EXISTS(SELECT 1 FROM \"{TableName}\" WHERE \"Item\" = @Item LIMIT 1);";
            var query = $"SELECT 1 FROM \"{TableName}\" WHERE \"Item\" = @Item LIMIT 1";
            var result = await Connection.QueryFirstOrDefaultAsync<BlacklistItemEntity>(query, obj)
                .ConfigureAwait(false);
            return result != null;
        }

        public async Task<bool> ContainsItemsAsync(IEnumerable<string> items)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            var enumerable = items as string[] ?? items.ToArray();
            if (enumerable.Length == 0)
                return false;

            var commaDelimitedKeys = SQLiteQueryBuilderHelpers.JoinCollectionWithCommaDelimter(
                enumerable
            );

            var query = $"SELECT * FROM \"{TableName}\" WHERE \"Item\" in ({commaDelimitedKeys});";
            var result = await Connection.QueryAsync<BlacklistItemEntity>(query);
            return result.Count() == enumerable.Count();
        }
    }
}
