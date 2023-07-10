using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{
    public class TumblrConversationParticipant
    {
        [JsonProperty("admin")]
        public bool Admin { get; set; }

        [JsonProperty("can_message")]
        public bool CanMessage { get; set; }

        [JsonProperty("is_adult")]
        public bool IsAdult { get; set; }

        [JsonProperty("messaging_allow_follows_only")]
        public bool MessagingAllowFollowsOnly { get; set; }

        [JsonProperty("seconds_since_last_activity")]
        public int SecondsSinceLastActivity { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }

        [JsonProperty("is_active")]
        public bool IsActive { get; set; }
    }

    public class TumblrConversationContent
    {

        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("formatting")]
        public IList<object> Formatting { get; set; }
    }

    public class TumblrConversationDatum
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
        public TumblrConversationContent Content { get; set; }
    }

    public class TumblrConversationMessages
    {

        [JsonProperty("data")]
        public IList<TumblrConversationDatum> Data { get; set; }
    }

    public class RetrieveConversationResponse
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
        public IList<TumblrConversationParticipant> Participants { get; set; }

        [JsonProperty("messages")]
        public TumblrConversationMessages Messages { get; set; }
    }

    public class RetrieveConversationTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveConversationResponse Response { get; set; }
    }
}
