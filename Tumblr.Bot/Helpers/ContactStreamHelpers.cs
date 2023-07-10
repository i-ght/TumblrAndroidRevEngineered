using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Tumblr.Bot.SQLite;
using Waifu.Sys;

namespace Tumblr.Bot.Helpers
{
    internal static class ContactStreamHelpers
    {
        public static async Task AdvanceContactStreamReaderToAnItemNotBlacklisted(
            StreamReader contactStreamReader,
            SQLiteDb db)
        {
            if (contactStreamReader == null)
                return;

            long prevPosition = 0;
            var tmp = new List<string>();

            var charPosFieldInfo = contactStreamReader.GetType().GetField(
                "charPos",
                BindingFlags.DeclaredOnly |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.GetField
            );
            var charLenFieldInfo = contactStreamReader.GetType().GetField(
                "charLen",
                BindingFlags.DeclaredOnly |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.GetField
            );

            if (charPosFieldInfo == null)
            {
                throw new InvalidOperationException(
                    "failed to get charPos field."
                );
            }
            if (charLenFieldInfo == null)
            {
                throw new InvalidOperationException(
                    "failed to get charLen field."
                );
            }

            while (!contactStreamReader.EndOfStream)
            {
                if (tmp.Count == 1000)
                {
                    if (!await db.GreetBlacklistTable.ContainsItemsAsync(tmp)
                        .ConfigureAwait(false))
                    {
                        contactStreamReader.DiscardBufferedData();
                        contactStreamReader.BaseStream.Seek(prevPosition, SeekOrigin.Begin);
                        tmp.Clear();
                        break;
                    }

                    var charPos = (int)charLenFieldInfo.GetValue(contactStreamReader);
                    var charLen = (int)charPosFieldInfo.GetValue(contactStreamReader);
                    prevPosition = contactStreamReader.BaseStream.Position -
                                   charPos
                                   + charLen;
                    tmp.Clear();
                }

                var line = await contactStreamReader.ReadLineAsync()
                    .ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var split = line.Split('|');
                if (split.Length < 2)
                    continue;

                if (StringHelpers.AnyNullOrEmpty(split))
                    continue;

                var username = split[0];
                tmp.Add(username);
            }

            using (var transaction = db.GreetBlacklistTable.BeginTransaction())
            {
                try
                {
                    while (!contactStreamReader.EndOfStream)
                    {
                        var charPos = (int)charLenFieldInfo.GetValue(contactStreamReader);
                        var charLen = (int)charPosFieldInfo.GetValue(contactStreamReader);
                        prevPosition = contactStreamReader.BaseStream.Position -
                                       charPos
                                       + charLen;

                        var line = await contactStreamReader.ReadLineAsync()
                            .ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        var split = line.Split('|');
                        if (split.Length < 2)
                            continue;

                        if (StringHelpers.AnyNullOrEmpty(split))
                            continue;

                        var username = split[0];

                        if (await db.GreetBlacklistTable.ContainsItemAsync(username)
                            .ConfigureAwait(false))
                        {
                            continue;
                        }

                        contactStreamReader.DiscardBufferedData();
                        contactStreamReader.BaseStream.Seek(prevPosition, SeekOrigin.Begin);
                        break;
                    }

                    transaction.Commit();
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
