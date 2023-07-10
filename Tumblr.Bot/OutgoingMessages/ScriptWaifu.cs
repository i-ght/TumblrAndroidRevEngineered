using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Waifu.Collections;
using Waifu.Sys;

namespace Tumblr.Bot.OutgoingMessages
{
    public class ScriptWaifu
    {
        private readonly ReadOnlyCollection<string> _script;
        private readonly List<string> _keywordsRespondedTo;
        private readonly List<DateTime> _sentKeepAlives;

        /// <summary>
        /// Creates a new script waifu with specified script
        /// </summary>
        /// <param name="script"></param>
        /// <param name="scriptIndex"></param>
        public ScriptWaifu(ReadOnlyCollection<string> script, int scriptIndex = -1)
        {
            if (script == null)
                throw new ArgumentNullException(nameof(script));

            if (script.Count == 0)
                throw new ArgumentException(@"script list was empty", nameof(script));

            _script = script;
            _keywordsRespondedTo = new List<string>();
            _sentKeepAlives = new List<DateTime>();
            ScriptIndex = scriptIndex;
        }

        public bool Pending { get; set; }
        public bool IsFirstLine => ScriptIndex == -1;
        public bool IsComplete => string.IsNullOrWhiteSpace(PreviewNextLine());
        public DateTime LastMessageReceivedAt { get; set; }
        public int KeepAlivesSent { get; set; }
        public bool HaveSentKeepAlive => _sentKeepAlives.Contains(LastMessageReceivedAt);
        public int ScriptIndex { get; private set; }

        public void OnSentKeepAlive()
        {
            KeepAlivesSent++;
            _sentKeepAlives.Add(LastMessageReceivedAt);
        }

        /// <summary>
        /// Returns next line of the script 
        /// </summary>
        /// <returns></returns>
        public string NextLine()
        {
            return ++ScriptIndex >= _script.Count ? string.Empty : Spin(_script[ScriptIndex]);
        }

        /// <summary>
        /// Returns next line of script unspun
        /// </summary>
        /// <returns></returns>
        public string NextLineUnSpun()
        {
            return ++ScriptIndex >= _script.Count ? string.Empty : _script[ScriptIndex];
        }

        /// <summary>
        /// Returns a preview of the next line
        /// </summary>
        /// <returns></returns>
        private string PreviewNextLine()
        {
            return ScriptIndex + 1 >= _script.Count ? string.Empty : Spin(_script[ScriptIndex + 1]);
        }

        /// <summary>
        /// Spins the input string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Spin(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return Regex.Replace(input, "{(.*?)}", delegate (Match match)
            {
                if (!match.Groups[1].Value.Contains("|"))
                    return match.Groups[1].Value;

                var splt = match.Groups[1].Value.Split('|');
                var ret = splt[ThreadSafeStaticRandom.RandomInt(splt.Length)];
                return ret;
            });
        }

        public bool TryFindKeywordResponse(string message, Dictionary<string, List<string>> keywords, out string response)
        {
            response = string.Empty;

            if (string.IsNullOrWhiteSpace(message) ||
                keywords == null ||
                keywords.Count == 0)
            {
                return false;
            }

            message = StringHelpers.RemoveNonAlphaNumericsAndNonWhiteSpace(message);
            if (string.IsNullOrWhiteSpace(message))
                return false;

            message = message.ToLower();

            foreach (var k in keywords.Keys)
            {
                if (string.IsNullOrWhiteSpace(k) ||
                    keywords[k] == null ||
                    keywords[k].Count == 0)
                {
                    continue;
                }

                var keyword = k.ToLower();
                var wordSplit = k.Split(' ');
                if (wordSplit.Length == 1)
                {
                    var msgWordSplit = message.Split(' ');
                    foreach (var word in msgWordSplit)
                    {
                        if (word != keyword)
                            continue;

                        if (_keywordsRespondedTo.Contains(keyword))
                            return false;

                        _keywordsRespondedTo.Add(k);
                        response = GetKeywordResponse(k, keywords);
                        return !string.IsNullOrWhiteSpace(response);
                    }

                    continue;
                }

                if (!message.Contains(keyword))
                    continue;

                if (_keywordsRespondedTo.Contains(keyword))
                    return false;

                _keywordsRespondedTo.Add(k);
                response = GetKeywordResponse(k, keywords);
                return !string.IsNullOrWhiteSpace(response);
            }

            return false;
        }

        private static string GetKeywordResponse(string keyword, Dictionary<string, List<string>> keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword) ||
                keywords == null ||
                keywords.Count == 0 ||
                !keywords.ContainsKey(keyword))
            {
                return string.Empty;
            }

            return keywords[keyword].RandomSelection();
        }

        public static bool HasRestrictedKeyword(string message, List<string> restricts)
        {
            if (string.IsNullOrWhiteSpace(message) ||
                restricts == null ||
                restricts.Count == 0)
            {
                return false;
            }

            message = StringHelpers.RemoveNonAlphaNumericsAndNonWhiteSpace(message);
            if (string.IsNullOrWhiteSpace(message))
                return false;
            message = message.ToLower();

            foreach (var r in restricts)
            {
                var keyword = r.ToLower();

                var split = r.Split(' ');
                if (split.Length == 1)
                {
                    var msgSplit = message.Split(' ');
                    foreach (var word in msgSplit)
                        if (word == keyword)
                            return true;
                }
                else
                {
                    if (message.Contains(keyword))
                        return true;
                }
            }

            return false;
        }
    }
}
