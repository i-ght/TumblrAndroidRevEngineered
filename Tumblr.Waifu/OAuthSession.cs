namespace Tumblr.Waifu
{
    public class OAuthSession
    {
        public OAuthSession(
            string oAuthToken,
            string oAuthTokenSecret)
        {
            OAuthToken = oAuthToken;
            OAuthTokenSecret = oAuthTokenSecret;
        }

        public string OAuthToken { get; }
        public string OAuthTokenSecret { get; }

        public override string ToString()
        {
            return $"{OAuthToken}:{OAuthTokenSecret}";
        }
    }
}
