using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{

    public class RetrieveNoticesResponse
    {
    }

    public class RetrieveNoticesTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveNoticesResponse Response { get; set; }
    }
}
