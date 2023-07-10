using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Waifu.Sys;

namespace Tumblr.Waifu
{
    public static class BCookieFactory
    {
        //NOTE: Located at com.yahoo.uda.yi13n.YQLProxy, com.yahoo.uda.yi13n.ULTUtils

        private static readonly char[] EncodeArray;

        static BCookieFactory()
        {
            EncodeArray = new[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b',
                'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
                't', 'u', 'v'
            };
        }

        private static string PadLeadingZeros(string input, int numOfTotalChars)
        {
            int v0;
            var v2 = new StringBuilder();
            if (string.IsNullOrEmpty(input))
            {
                for (v0 = 0; v0 < numOfTotalChars; ++v0)
                    v2.Append("0");

                input = v2.ToString();
            }
            else if (input.Length < numOfTotalChars)
            {
                var v1 = numOfTotalChars - input.Length;
                for (v0 = 0; v0 < v1; ++v0)
                    v2.Append("0");

                v2.Append(input);
                input = v2.ToString();
            }

            return input;
        }


        public static string GetBCookie(string xsId)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                var hash = sha1.ComputeHash(Convert.FromBase64String(xsId));
                var hex = ByteArrayHelpers.ByteArrayToHex(hash);
                xsId = hex;
            }

            var sb = new StringBuilder();

            var subStr1 = xsId.Substring(0, 8);
            var subStr2 = xsId.Substring(8, 8);
            var binary1 = Convert.ToString(long.Parse(subStr1, NumberStyles.HexNumber), 2);
            var binary2 = Convert.ToString(long.Parse(subStr2, NumberStyles.HexNumber), 2);
            var part1 = PadLeadingZeros(binary1, 33);
            var part2 = PadLeadingZeros(binary2, 32);
            var combined = part1 + part2;

            for (var i = 1; i < 14; ++i)
            {
                var startIndex = (i - 1) * 5;
                var endIndex = i * 5;
                var len = endIndex - startIndex;

                var forsenXWutFace = Convert.ToInt32(combined.Substring(startIndex, len), 2);
                sb.Append(EncodeArray[forsenXWutFace]);
            }

            var ret = sb.ToString();
            return ret;
        }
    }
}
