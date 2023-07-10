using System;
using System.Threading.Tasks;
using Tumblr.Bot.SQLite.AccessProviders;

namespace Tumblr.Bot.SQLite
{
    internal class SQLiteDb : IDisposable
    {
        private bool _disposed;

        public SQLiteDb(
            ChatBlacklistDbTableAccessProvider chatBlacklistTable,
            ScriptsDbTableAccessProvider scriptsTable,
            ConversationStatesDbTableAccessProvider convoStatesTable,
            GreetBlacklistDbTableAccessProvider greetBlacklistTable)
        {
            if (chatBlacklistTable == null)
                throw new ArgumentNullException(nameof(chatBlacklistTable));

            if (scriptsTable == null)
                throw new ArgumentNullException(nameof(scriptsTable));

            if (convoStatesTable == null)
                throw new ArgumentNullException(nameof(convoStatesTable));

            ChatBlacklistTable = chatBlacklistTable;
            ScriptsTable = scriptsTable;
            ConvoStatesTable = convoStatesTable;
            GreetBlacklistTable = greetBlacklistTable;
        }

        public ChatBlacklistDbTableAccessProvider ChatBlacklistTable { get; }
        public ScriptsDbTableAccessProvider ScriptsTable { get; }
        public ConversationStatesDbTableAccessProvider ConvoStatesTable { get; }
        public GreetBlacklistDbTableAccessProvider GreetBlacklistTable { get; }

        public async Task InitializeAsync()
        {
            await ChatBlacklistTable.InitializeAsync()
                .ConfigureAwait(false);

            await ScriptsTable.InitializeAsync()
                .ConfigureAwait(false);

            await ConvoStatesTable.InitializeAsync()
                .ConfigureAwait(false);

            await GreetBlacklistTable.InitializeAsync()
                .ConfigureAwait(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                ChatBlacklistTable.Dispose();
                ScriptsTable.Dispose();
                ConvoStatesTable.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
