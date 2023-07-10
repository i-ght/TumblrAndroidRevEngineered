using System;
using System.Security.Cryptography;
using System.Text;

namespace Tumblr.Waifu
{
    public static class Crypto
    {
        public static string TumblrRegisterSignature(string username, string nonce, string password, string email)
        {
            const string beginningSalt = "lKGxL2Pian";
            const string endingSalt = "DDU0veZwpJ";
            const string key = "nME7QXhBJruAi8uuWi80Rkl5+ULgHHSGpFx3dxYDvTaRlDrHZeXu5XTMzceTI1JYja/OSUE+hk7oQGhfVfAIwBhc8wb0FhSYqyj+";

            var input = $"{beginningSalt}{username}{nonce}{password}{email}{endingSalt}";
            var signature = HmacSha1Base64(key, input);
            return signature;
        }

        private static string HmacSha1Base64(string key, string input)
        {
            var keyData = Encoding.UTF8.GetBytes(key);
            var inputData = Encoding.UTF8.GetBytes(input);

            using (var hmacSha1 = new HMACSHA1(keyData))
            {
                var hash = hmacSha1.ComputeHash(inputData);
                var base64 = Convert.ToBase64String(hash);
                return base64;
            }
        }
    }
}
