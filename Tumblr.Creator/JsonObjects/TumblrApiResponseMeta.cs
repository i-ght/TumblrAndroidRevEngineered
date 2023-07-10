using Newtonsoft.Json;

namespace Tumblr.Creator.JsonObjects
{
    internal class TumblrApiResponseMeta
    {

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }
}
