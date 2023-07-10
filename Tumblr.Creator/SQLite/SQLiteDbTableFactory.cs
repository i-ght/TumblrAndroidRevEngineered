using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Tumblr.Creator.SQLite
{
    internal static class SQLiteDbTableFactory
    {
        public static EmailBlacklistAccessProvider CreateBlacklistTable()
        {
            var typeofDateTime = typeof(DateTime);

            var dict = new Dictionary<string, string>
            {
                ["Index"] = typeof(int).Name,
                ["LoginId"] = typeof(string).Name,
                ["CreatedAt"] = typeofDateTime.Name,
                ["LastModifiedAt"] = typeofDateTime.Name,
            };

            var columns = new ReadOnlyDictionary<string, string>(dict);
            var ret = new EmailBlacklistAccessProvider(
                "db.sqlite",
                "YandexBlacklist",
                "Index",
                columns
            );
            return ret;
        }
    }
}
