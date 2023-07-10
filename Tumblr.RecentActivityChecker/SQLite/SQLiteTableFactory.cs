using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tumblr.RecentActivityChecker.SQLite
{
    internal static class SQLiteTableFactory
    {
        public static ContactBlacklistSQLiteTable CreateKeywordBlacklistTable()
        {
            var typeofDateTime = typeof(DateTime);

            var dict = new Dictionary<string, string>
            {
                ["Index"] = typeof(int).Name,
                ["Item"] = typeof(string).Name,
                ["CreatedAt"] = typeofDateTime.Name,
                ["LastModifiedAt"] = typeofDateTime.Name,
            };

            var columns = new ReadOnlyDictionary<string, string>(dict);
            var ret = new ContactBlacklistSQLiteTable(
                "db.sqlite",
                "ContactBlacklist",
                "Index",
                columns
            );
            return ret;
        }
    }
}
