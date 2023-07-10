using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Tumblr.Bot.SQLite
{
    internal static class DbHelpers
    {
        private static readonly Regex SplitNewLineRegex;

        static DbHelpers()
        {
            SplitNewLineRegex = new Regex("\r\n", RegexOptions.Compiled);
        }

        public static string ListAsString(IReadOnlyCollection<string> list)
        {
            var str = string.Join(Environment.NewLine, list);
            return str;
        }

        public static string CalculateScriptKey(string scriptLines)
        {
            var sha256Sum = Sha256Hex(scriptLines);
            return sha256Sum;
        }

        public static ReadOnlyCollection<string> ScriptLinesToList(string scriptLines)
        {
            var lines = SplitNewLineRegex.Split(scriptLines);
            return new List<string>(lines).AsReadOnly();
        }

        private static string Sha256Hex(string input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException(
                    $"{nameof(input)} must not be whitespace."
                );
            }

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                var hex = BytesToHex(hash);
                return hex;
            }
        }

        private static string BytesToHex(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            if (input.Length == 0)
            {
                throw new ArgumentException(
                    $@"{nameof(input)} must have a length > 0.",
                    nameof(input)
                );
            }

            var sb = new StringBuilder();
            foreach (var @byte in input)
                sb.Append(@byte.ToString("x2"));

            var ret = sb.ToString();
            return ret;
        }
    }
}
