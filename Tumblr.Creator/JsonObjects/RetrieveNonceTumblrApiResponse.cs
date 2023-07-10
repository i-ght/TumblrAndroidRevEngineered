using Newtonsoft.Json;

namespace Tumblr.Creator.JsonObjects
{
    internal class RetrieveNonceResponse
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }

    internal class RetrieveNonceTumblrApiResponse
    {
        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveNonceResponse Response { get; set; }
    }
}
