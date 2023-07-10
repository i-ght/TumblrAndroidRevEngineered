using Waifu.SQLite;
using Waifu.SQLite.Attributes;

namespace Tumblr.Scraper.SQLite
{
    internal class BlacklistItemEntity : SQLiteEntity<int?>
    {
        public BlacklistItemEntity()
        {
        }

        public BlacklistItemEntity(string item)
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
