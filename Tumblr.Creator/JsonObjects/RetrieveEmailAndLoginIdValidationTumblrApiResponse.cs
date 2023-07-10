using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Creator.JsonObjects
{
    internal class TumblrValidationError
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public int Code { get; set; }
    }

    internal class RetrieveEmailAndLoginIdValidationResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("tumblelog_errors")]
        public IList<TumblrValidationError> TumblelogErrors { get; set; }

        [JsonProperty("user_errors")]
        public IList<TumblrValidationError> UserErrors { get; set; }
    }

    internal class RetrieveEmailAndLoginIdValidationTumblrApiResponse
    {
        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public RetrieveEmailAndLoginIdValidationResponse Response { get; set; }
    }
}
