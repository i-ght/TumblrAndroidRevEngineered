using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{
    public class RetrieveBlogInfoBlog
    {
        [JsonProperty("can_message")]
        public bool CanMessage { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("seconds_since_last_activity")]
        public int SecondsSinceLastActivity { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("is_optout_ads")]
        public bool IsOptoutAds { get; set; }
    }

    public class RetrieveBlogInfoResponse
    {

        [JsonProperty("blog")]
        public RetrieveBlogInfoBlog Blog { get; set; }
    }

    public class RetrieveBlogInfoTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveBlogInfoResponse Response { get; set; }
    }
}
