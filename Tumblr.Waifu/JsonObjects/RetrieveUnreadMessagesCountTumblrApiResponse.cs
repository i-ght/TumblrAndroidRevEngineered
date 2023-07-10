using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{


    public class UnreadMessagesCountResponse
    {
        [JsonProperty("unread_messages_count")]
        public Dictionary<string, Dictionary<string, int>> UnreadMessages { get; set; }
    }

        public class RetrieveUnreadMessagesCountTumblrApiResponse
    {
        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public UnreadMessagesCountResponse Response { get; set; }
    }
}
