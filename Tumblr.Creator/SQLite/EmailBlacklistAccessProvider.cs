using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Waifu.SQLite;

namespace Tumblr.Creator.SQLite
{
    internal class EmailBlacklistAccessProvider : SQLiteDbTableAccessProvider<EmailBlacklistEntity, int?>
    {
        public EmailBlacklistAccessProvider(
            string dbFileName,
            string tableName,
            string primaryKeyColumnName,
            IReadOnlyDictionary<string, string> columns)
            : base(dbFileName, tableName, primaryKeyColumnName, columns)
        {
        }

        public async Task<bool> ContainsLoginIdAsync(string loginId)
        {
            if (loginId == null)
                throw new ArgumentNullException(nameof(loginId));

            var obj = new { LoginId = loginId };
            var query = $"SELECT 1 FROM \"{TableName}\" WHERE \"LoginId\" = @LoginId LIMIT 1";
            var result = await Connection.QueryFirstOrDefaultAsync<EmailBlacklistEntity>(query, obj)
                .ConfigureAwait(false);
            return result != null;
        }
    }
}