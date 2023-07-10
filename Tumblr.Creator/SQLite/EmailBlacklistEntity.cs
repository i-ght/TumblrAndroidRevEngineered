using Waifu.SQLite;
using Waifu.SQLite.Attributes;

namespace Tumblr.Creator.SQLite
{
    internal class EmailBlacklistEntity : SQLiteEntity<int?>
    {
        public EmailBlacklistEntity() { }

        public EmailBlacklistEntity(string loginId)
        {
            LoginId = loginId;
        }

        public override int? Key
        {
            get => Index;
            set => Index = value;
        }

        [SQLitePrimaryKey]
        public int? Index { get; set; }

        [SQLiteIndex]
        public string LoginId { get; set; }
    }
}
