
using System.Collections.Generic;
using Waifu.Sys;

namespace Tumblr.Waifu
{
    public class TumblrAccount
    {
        public TumblrAccount(
            string username,
            string email,
            string password,
            string emailPassword,
            string blogUrl,
            //string uuid,
            TumblrSessionInfo sessionInfo,
            OAuthSession oAuthSession)
        {
            Username = username;
            Email = email;
            Password = password;
            EmailPassword = emailPassword;
            BlogUrl = blogUrl;
            SessionInfo = sessionInfo;
            OAuthSession = oAuthSession;
            //Uuid = uuid;
        }

        public string Username { get; }
        public string Email { get; }
        public string Password { get; }
        public string EmailPassword { get; }
        public TumblrSessionInfo SessionInfo { get; }
        public OAuthSession OAuthSession { get; }
        public string BlogUrl { get; }
        public string Uuid { get; set; }

        public override string ToString()
        {
            return $"{Username}:{Email}:{Password}:{EmailPassword}:{BlogUrl}:{SessionInfo}:{OAuthSession}";
        }

        public static bool TryParse(
            string input,
            out TumblrAccount tumblrAccount)
        {
            tumblrAccount = null;
            if (string.IsNullOrWhiteSpace(input) ||
                !input.Contains(":")) //||
                //!input.Contains("~") ||
                //!input.Contains("|"))
            {
                return false;
            }

            var split = input.Split(':');
            if (split.Length != 15)
                return false;

            if (StringHelpers.AnyNullOrEmpty(split))
                return false;

            var email = split[1];

            if (!email.Contains("@")) {

                email = split[0];
                var password = split[1];
                var username = split[2];

                var xId = split[3];
                var xIdDate = split[4];
                var xsId = split[5];
                var bCookie = split[7];
                var xyUserAgent = split[8];
                var deviceInfo = "DI/1.0 (311; 090; [WIFI])";
                var tumblrDevStr = "samsung|SM-N910W8|4.4.4|KTU84P";
                var carrierInfoStr = "T-Mobile~310~260";

                var lst = new List<string>
                {
                    xId,
                    xIdDate,
                    xsId,
                    bCookie,
                    xyUserAgent,
                    deviceInfo,
                    tumblrDevStr,
                    carrierInfoStr
                };

                var sessionInfoStr = string.Join(":", lst);
                if (!TumblrSessionInfo.TryParse(sessionInfoStr, out var sessionInfo))
                    return false;

                var oAuthSession = new OAuthSession(
                    split[9],
                    split[10]
                );

                tumblrAccount = new TumblrAccount(
                    username,
                    email,
                    password,
                    "",
                    $"{username}.tumblr.com",
                    sessionInfo,
                    oAuthSession
                );

                return true;

            }
            else {
                var username = split[0];
                //var email = split[1];
                var password = split[2];
                var emailPassword = split[3];
                var blogUrl = split[4];
                var xId = split[5];
                var xIdDate = split[6];
                var xsId = split[7];
                var bCookie = split[8];
                var xyUserAgent = split[9];
                var deviceInfo = split[10];
                var tumblrDevStr = split[11];
                var carrierInfoStr = split[12];

                var lst = new List<string>
                {
                    xId,
                    xIdDate,
                    xsId,
                    bCookie,
                    xyUserAgent,
                    deviceInfo,
                    tumblrDevStr,
                    carrierInfoStr
                };

                var sessionInfoStr = string.Join(":", lst);
                if (!TumblrSessionInfo.TryParse(sessionInfoStr, out var sessionInfo))
                    return false;

                var oAuthSession = new OAuthSession(
                    split[13],
                    split[14]
                );

                tumblrAccount = new TumblrAccount(
                    username,
                    email,
                    password,
                    emailPassword,
                    blogUrl,
                    sessionInfo,
                    oAuthSession
                );

                return true;
            }
        }
    }
}
