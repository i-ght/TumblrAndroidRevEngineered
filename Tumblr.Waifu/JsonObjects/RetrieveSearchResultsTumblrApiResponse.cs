using System.Collections.Generic;
using Newtonsoft.Json;

namespace Tumblr.Waifu.JsonObjects
{
    public class SearchResultsResource
    {
        [JsonProperty("can_message")]
        public bool CanMessage { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("seconds_since_last_activity")]
        public int SecondsSinceLastActivity { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("uuid")]
        public string Uuid { get; set; }
    }

    public class SearchResultsElement
    {

        [JsonProperty("object_type")]
        public string ObjectType { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("serve_id")]
        public string ServeId { get; set; }

        [JsonProperty("recommendation_reason")]
        public object RecommendationReason { get; set; }

        [JsonProperty("dismissal")]
        public object Dismissal { get; set; }

        [JsonProperty("resources")]
        public IList<SearchResultsResource> Resources { get; set; }

        [JsonProperty("resource_ids")]
        public IList<string> ResourceIds { get; set; }
    }

    public class SearchResultsTimeline
    {

        [JsonProperty("elements")]
        public IList<SearchResultsElement> Elements { get; set; }

        [JsonProperty("_links")]
        public SearchResultsLinks Links { get; set; }
    }

    public class SearchResultsQueryParams
    {

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("timeline_type")]
        public string TimelineType { get; set; }

        [JsonProperty("block_nsfw")]
        public string BlockNsfw { get; set; }

        [JsonProperty("cursor")]
        public string Cursor { get; set; }
    }

    public class SearchResultsNext
    {

        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("query_params")]
        public SearchResultsQueryParams QueryParams { get; set; }
    }

    public class SearchResultsLinks
    {

        [JsonProperty("next")]
        public SearchResultsNext Next { get; set; }
    }

    public class SearchResultsResponse
    {

        [JsonProperty("timeline")]
        public SearchResultsTimeline Timeline { get; set; }

        [JsonProperty("supply_logging_positions")]
        public IList<object> SupplyLoggingPositions { get; set; }

        [JsonProperty("psa")]
        public object Psa { get; set; }
    }

    public class RetrieveSearchResultsTumblrApiResponse
    {

        [JsonProperty("meta")]
        public TumblrApiResponseMeta Meta { get; set; }

        [JsonProperty("response")]
        public SearchResultsResponse Response { get; set; }
    }
}
