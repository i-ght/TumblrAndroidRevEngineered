using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Tumblr.Bot.SQLite.Entities;
using Waifu.SQLite;

namespace Tumblr.Bot.SQLite.AccessProviders
{
    public class ConversationStatesDbTableAccessProvider :
        SQLiteDbTableAccessProvider<ConversationStateEntity, string>
    {
        public ConversationStatesDbTableAccessProvider(
            string dbFileName,
            string tableName,
            string primaryKeyColumnName,
            IReadOnlyDictionary<string, string> columns
        ) : base(dbFileName, tableName, primaryKeyColumnName, columns)
        {
        }

        public async Task<IEnumerable<ConversationStateEntity>> RetrieveByBotUsername(
            string botUsername)
        {
            if (botUsername == null)
                throw new ArgumentNullException(nameof(botUsername));

            if (string.IsNullOrWhiteSpace(botUsername))
            {
                throw new ArgumentException(
                    $@"{nameof(botUsername)} must not be whitespace.",
                    nameof(botUsername)
                );
            }

            using (var transaction = Connection.BeginTransaction())
            {
                try
                {
                    var obj = new { BotUsername = botUsername };
                    var query = $"SELECT * FROM \"{TableName}\" WHERE \"BotUsername\" = @BotUsername;";
                    var result = await Connection.QueryAsync<ConversationStateEntity>(
                        query,
                        obj,
                        transaction
                    ).ConfigureAwait(false);
                    transaction.Commit();
                    return result;
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
