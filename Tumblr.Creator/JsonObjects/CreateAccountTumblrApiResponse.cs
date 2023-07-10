using Newtonsoft.Json;

namespace Tumblr.Creator.JsonObjects
{
    public class CreateAccountResponseOnboarding
    {
        [JsonProperty("session_id")]
        public string SessionId { get; set; }
    }

    public class CreateAccountResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("onboarding")]
        public CreateAccountResponseOnboarding Onboarding { get; set; }
    }

    internal class CreateAccountTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public CreateAccountResponse Response { get; set; }
    }
}
