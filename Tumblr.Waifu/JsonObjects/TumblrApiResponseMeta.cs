using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{
    public class TumblrApiResponseMeta
    {

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

}
