using Waifu.SQLite;
using Waifu.SQLite.Attributes;

namespace Tumblr.RecentActivityChecker.SQLite
{
    internal class BlacklistEntity : SQLiteEntity<int?>
    {
        public BlacklistEntity()
        {
        }

        public BlacklistEntity(string item)
        {
            Item = item;
        }

        public override int? Key
        {
            get => Index;
            set => Index = value;
        }

        public int? Index { get; set; }

        [SQLiteIndex]
        public string Item { get; set; }
    }
}
