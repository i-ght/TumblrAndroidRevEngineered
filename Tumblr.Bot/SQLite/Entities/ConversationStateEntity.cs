using Waifu.SQLite;
using Waifu.SQLite.Attributes;

namespace Tumblr.Bot.SQLite.Entities
{
    public class ConversationStateEntity : SQLiteEntity<string>
    {
        public ConversationStateEntity() { }

        public ConversationStateEntity(
            string botUsername,
            string contactUsername,
            string botUuid,
            string contactUuid,
            string conversationId,
            string scriptSha256Sum,
            int scriptIndex = -1)
        {
            Id = $"{botUsername}~{contactUsername}";
            BotUsername = botUsername;
            ContactUsername = contactUsername;
            BotUuid = botUuid;
            ContactUuid = contactUuid;
            ConversationId = conversationId;
            ScriptSha256Sum = scriptSha256Sum;
            ScriptIndex = scriptIndex;
        }

        [SQLitePrimaryKey]
        public string Id { get; set; }

        [SQLiteForeignKey("Scripts", "Sha256Sum", "ScriptSha256Sum")]
        public string ScriptSha256Sum { get; set; }

        [SQLiteIndex]
        public string BotUsername { get; set; }

        [SQLiteIndex]
        public string BotUuid { get; set; }

        [SQLiteIndex]
        public string ContactUuid { get; set; }

        [SQLiteIndex]
        public string ContactUsername { get; set; }

        [SQLiteIndex]
        public string ConversationId { get; set; }

        public int ScriptIndex { get; set; }

        public bool IsComplete { get; set; }

        public override string Key
        {
            get => ScriptSha256Sum;
            set => ScriptSha256Sum = value;
        }
    }
}
