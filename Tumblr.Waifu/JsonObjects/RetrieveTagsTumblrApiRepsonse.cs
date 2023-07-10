using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{
    public class TumblrTag
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("thumb_url")]
        public string ThumbUrl { get; set; }

        [JsonProperty("unread_count")]
        public object UnreadCount { get; set; }
    }

    public class TagsQueryParams
    {

        [JsonProperty("force_oauth")]
        public string ForceOauth { get; set; }

        [JsonProperty("offset")]
        public string Offset { get; set; }
    }

    public class TagsNext
    {

        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("query_params")]
        public TagsQueryParams QueryParams { get; set; }
    }

    public class TagsLinks
    {

        [JsonProperty("next")]
        public TagsNext Next { get; set; }
    }

    public class RetrieveTagsResponse
    {

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("tags")]
        public IList<TumblrTag> Tags { get; set; }

        [JsonProperty("_links")]
        public TagsLinks Links { get; set; }
    }

    public class RetrieveTagsTumblrApiRepsonse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveTagsResponse Response { get; set; }
    }

}
