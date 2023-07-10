using Tumblr.Bot.OutgoingMessages;

namespace Tumblr.Bot.Shikaka.Messages.SQLite
{
    internal class UpdateConvoEntityMessage
    {
        public UpdateConvoEntityMessage(
            string accountUsername,
            OutgoingMessage outgoingMessage,
            ScriptWaifu scriptWaifu)
        {
            AccountUsername = accountUsername;
            OutgoingMessage = outgoingMessage;
            ScriptWaifu = scriptWaifu;
        }

        public string AccountUsername { get; }
        public OutgoingMessage OutgoingMessage { get; }
        public ScriptWaifu ScriptWaifu { get; }
    }
}
