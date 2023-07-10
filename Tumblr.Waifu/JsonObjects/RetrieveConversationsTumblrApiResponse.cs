using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{
    public class ConvoParticipant
    {
        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("can_message")]
        public bool CanMessage { get; set; }

        [JsonProperty("messages")]
        public int Messages { get; set; }

        [JsonProperty("messaging_allow_follows_only")]
        public bool MessagingAllowFollowsOnly { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }

    }

    public class ConvoContent
    {

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("formatting")]
        public IList<object> Formatting { get; set; }
    }

    public class ConvoDatum
    {

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("ts")]
        public string Ts { get; set; }

        [JsonProperty("participant")]
        public string Participant { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("content")]
        public ConvoContent Content { get; set; }
    }

    public class ConvoMessages
    {

        [JsonProperty("data")]
        public IList<ConvoDatum> Data { get; set; }
    }

    public class TumblrConversation
    {

        [JsonProperty("object_type")]
        public string ObjectType { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("last_modified_ts")]
        public int LastModifiedTs { get; set; }

        [JsonProperty("last_read_ts")]
        public int LastReadTs { get; set; }

        [JsonProperty("can_send")]
        public bool CanSend { get; set; }

        [JsonProperty("unread_messages_count")]
        public int UnreadMessagesCount { get; set; }

        [JsonProperty("is_possible_spam")]
        public bool IsPossibleSpam { get; set; }

        [JsonProperty("is_blurred_images")]
        public bool IsBlurredImages { get; set; }

        [JsonProperty("participants")]
        public IList<ConvoParticipant> Participants { get; set; }

        [JsonProperty("messages")]
        public ConvoMessages Messages { get; set; }
    }

    public class ConvoQueryParams
    {

        [JsonProperty("participant")]
        public string Participant { get; set; }

        [JsonProperty("before")]
        public string Before { get; set; }
        
        [JsonProperty("after")]
        public string After { get; set; }
    }

    public class ConvoNext
    {

        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("query_params")]
        public ConvoQueryParams QueryParams { get; set; }
    }

    public class ConvoLinks
    {

        [JsonProperty("next")]
        public ConvoNext Next { get; set; }
    }

    public class RetrieveConversationsResponse
    {

        [JsonProperty("conversations")]
        public List<TumblrConversation> Conversations { get; set; }

        [JsonProperty("_links")]
        public ConvoLinks Links { get; set; }
    }

    public class RetrieveConversationsTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveConversationsResponse Response { get; set; }
    }
}
