using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{
    public class TumblrSubTopic
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("featured")]
        public bool Featured { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("overlay")]
        public string Overlay { get; set; }

        [JsonProperty("text_style")]
        public string TextStyle { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("sub_topics")]
        public IList<TumblrSubTopic> SubTopics { get; set; }
    }

    public class TumblrTopic
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("tag")]
        public string Tag { get; set; }

        [JsonProperty("featured")]
        public bool Featured { get; set; }

        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }

        [JsonProperty("overlay")]
        public string Overlay { get; set; }

        [JsonProperty("text_style")]
        public string TextStyle { get; set; }

        [JsonProperty("background_color")]
        public string BackgroundColor { get; set; }

        [JsonProperty("sub_topics")]
        public IList<TumblrSubTopic> SubTopics { get; set; }
    }

    public class RetrieveTopicsResponse
    {

        [JsonProperty("topics")]
        public IList<TumblrTopic> Topics { get; set; }

        [JsonProperty("set_type")]
        public string SetType { get; set; }
    }

    public class RetrieveTopicsTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveTopicsResponse Response { get; set; }
    }
}
