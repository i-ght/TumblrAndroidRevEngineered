using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Waifu.SQLite;
using Waifu.SQLite.Helpers;

namespace Tumblr.RecentActivityChecker.SQLite
{
    internal class ContactBlacklistSQLiteTable :
        SQLiteDbTableAccessProvider<BlacklistEntity, int?>
    {
        public ContactBlacklistSQLiteTable(
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
            var result = await Connection.QueryFirstOrDefaultAsync<BlacklistEntity>(query, obj)
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

            using (var transaction = BeginTransaction())
            {
                try
                {
                    var commaDelimitedKeys = SQLiteQueryBuilderHelpers.JoinCollectionWithCommaDelimter(
                        enumerable
                    );

                    var query = $"SELECT * FROM \"{TableName}\" WHERE \"Item\" in ({commaDelimitedKeys});";
                    var result = await Connection.QueryAsync<BlacklistEntity>(query);
                    transaction.Commit();
                    return result.Count() == enumerable.Count();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

        }
    }
}
