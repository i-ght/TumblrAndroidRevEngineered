using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Waifu.Net.Http;
using Waifu.OAuth;

namespace Tumblr.Waifu
{
    public static class TumblrOAuthSessionCreator
    {
        private static readonly Regex SessionInfoRegex;

        static TumblrOAuthSessionCreator()
        {
            SessionInfoRegex = new Regex("oauth_token=(.*?)&oauth_token_secret=(.*?)\\Z",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public static async Task<OAuthSession> CreateOAuthSession(
            string email,
            string password,
            TumblrSessionInfo sessionInfo,
            HttpWaifu client)
        {
            if (email == null)
                throw new ArgumentNullException(nameof(email));

            if (password == null)
                throw new ArgumentNullException(nameof(password));

            if (sessionInfo == null)
                throw new ArgumentNullException(nameof(sessionInfo));

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException(
                    $"{nameof(email)} must be a valid email address.",
                    nameof(email)
                );
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(
                    $"{nameof(password)} must not be whitespace.",
                    nameof(password)
                );
            }

            const string url = "https://www.tumblr.com/oauth/access_token";

            var postParams = new HttpParameters
            {
                ["x_auth_username"] = email,
                ["x_auth_password"] = password,
                ["x_auth_mode"] = "client_auth"
            };

            var authHeader = TumblrHelpers.CreateOAuthHeader(
                OAuthHttpMethod.POST,
                url,
                postParams
            );

            var apiHeaders = TumblrHelpers.ApiHeaders(sessionInfo, authHeader);

            const HttpMethod httpMethod = HttpMethod.POST;
            var request = new HttpRequest(httpMethod, url)
            {
                AcceptEncoding = DecompressionMethods.GZip,
                OverrideUserAgent = TumblrConstants.UserAgent,
                OverrideHeaders = apiHeaders,
                ContentType = "application/x-www-form-urlencoded",
                ContentBody = postParams.ToUrlEncodedString()
            };

            var response = await client.SendRequestAsync(request)
                .ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw HttpHelpers.CreateHttpRequestFailedEx(
                    httpMethod,
                    url,
                    response.StatusCode,
                    response.StatusDescription,
                    client.Config.Proxy
                );
            }

            var oAuthToken = string.Empty;
            var oAuthTokenSecret = string.Empty;
            var match = SessionInfoRegex.Match(response.ContentBody);
            if (match.Success)
            {
                oAuthToken = match.Groups[1].Value;
                oAuthTokenSecret = match.Groups[2].Value;
            }

            if (string.IsNullOrWhiteSpace(oAuthToken))
            {
                throw new InvalidOperationException(
                    $"Failed to parse {nameof(oAuthToken)} in create oauth session response."
                );
            }

            if (string.IsNullOrWhiteSpace(oAuthTokenSecret))
            {
                throw new InvalidOperationException(
                    $"Failed to parse {nameof(oAuthTokenSecret)} in create oauth session response."
                );
            }

            var ret = new OAuthSession(oAuthToken, oAuthTokenSecret);
            return ret;
        }
    }
}
