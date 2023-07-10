using System;
using Waifu.SQLite;
using Waifu.SQLite.Attributes;

namespace Tumblr.Bot.SQLite.Entities
{
    public sealed class ScriptEntity : SQLiteEntity<string>
    {
        public ScriptEntity()
        {

        }

        public ScriptEntity(
            string sha256Sum,
            string scriptLines,
            int lineCount)
        {
            if (sha256Sum == null)
                throw new ArgumentNullException(nameof(sha256Sum));

            if (scriptLines == null)
                throw new ArgumentNullException(nameof(scriptLines));

            if (string.IsNullOrWhiteSpace(sha256Sum))
            {
                throw new ArgumentException(
                    $@"{nameof(sha256Sum)} must not be whitespace.",
                    nameof(sha256Sum)
                );
            }

            if (string.IsNullOrWhiteSpace(scriptLines))
            {
                throw new ArgumentException(
                    $@"{nameof(scriptLines)} must not be whitespace.",
                    nameof(scriptLines)
                );
            }

            Sha256Sum = sha256Sum;
            LineCount = lineCount;
            ScriptLines = scriptLines;
        }

        [SQLitePrimaryKey]
        public string Sha256Sum { get; set; }

        public string ScriptLines { get; set; }

        public int LineCount { get; set; }

        public override string Key
        {
            get => Sha256Sum;
            set => Sha256Sum = value;
        }
    }
}
