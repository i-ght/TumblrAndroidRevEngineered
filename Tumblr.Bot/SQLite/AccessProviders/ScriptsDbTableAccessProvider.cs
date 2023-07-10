using System.Collections.Generic;
using Tumblr.Bot.SQLite.Entities;
using Waifu.SQLite;

namespace Tumblr.Bot.SQLite.AccessProviders
{
    public class ScriptsDbTableAccessProvider :
        SQLiteDbTableAccessProvider<ScriptEntity, string>
    {
        public ScriptsDbTableAccessProvider(
            string dbFileName,
            string tableName,
            string primaryKeyColumnName,
            IReadOnlyDictionary<string, string> columns
        ) : base(dbFileName, tableName, primaryKeyColumnName, columns)
        {
        }
    }
}
