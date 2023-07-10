using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tumblr.Bot.SQLite.AccessProviders;

namespace Tumblr.Bot.SQLite
{
    internal static class SQLiteTableFactory
    {
        public static ChatBlacklistDbTableAccessProvider CreateChatBlacklistTable()
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
            var ret = new ChatBlacklistDbTableAccessProvider(
                "db.sqlite",
                "ChatBlacklist",
                "Index",
                columns
            );
            return ret;
        }

        public static GreetBlacklistDbTableAccessProvider CreateGreetBlacklistTable()
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
            var ret = new GreetBlacklistDbTableAccessProvider(
                "db.sqlite",
                "GreetBlacklist",
                "Index",
                columns
            );
            return ret;
        }


        public static ConversationStatesDbTableAccessProvider ConvoStatesTable()
        {
            var typeofString = typeof(string);
            var typeofDateTime = typeof(DateTime);

            var dict = new Dictionary<string, string>
            {
                ["Id"] = typeofString.Name,
                ["CreatedAt"] = typeofDateTime.Name,
                ["LastModifiedAt"] = typeofDateTime.Name,
                ["ScriptSha256Sum"] = typeofString.Name,
                ["BotUsername"] = typeofString.Name,
                ["ContactUsername"] = typeofString.Name,
                ["BotUuid"] = typeofString.Name,
                ["ContactUuid"] = typeofString.Name,
                ["ConversationId"] = typeofString.Name,
                ["ScriptIndex"] = typeof(int).Name,
                ["IsComplete"] = typeof(bool).Name,
            };

            var columns = new ReadOnlyDictionary<string, string>(dict);
            var ret = new ConversationStatesDbTableAccessProvider(
                "db.sqlite",
                "ConvoStates",
                "Id",
                columns
            );

            return ret;
        }

        public static ScriptsDbTableAccessProvider ScriptsTable()
        {
            var typeofString = typeof(string);
            var typeofDateTime = typeof(DateTime);
            var dict = new Dictionary<string, string>
            {
                ["Sha256Sum"] = typeofString.Name,
                ["CreatedAt"] = typeofDateTime.Name,
                ["LastModifiedAt"] = typeofDateTime.Name,
                ["ScriptLines"] = typeofString.Name,
                ["LineCount"] = typeof(int).Name
            };

            var columns = new ReadOnlyDictionary<string, string>(dict);

            var ret = new ScriptsDbTableAccessProvider(
                "db.sqlite",
                "Scripts",
                "Sha256Sum",
                columns
            );

            return ret;
        }
    }
}
