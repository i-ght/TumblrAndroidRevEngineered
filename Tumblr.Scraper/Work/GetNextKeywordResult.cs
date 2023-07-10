namespace Tumblr.Scraper.Work
{
    internal class GetNextKeywordResult
    {
        public GetNextKeywordResult(
            bool success)
        {
            Success = success;
        }

        public GetNextKeywordResult(
            bool success,
            string value)
        : this(success)
        {
            Value = value;
        }

        public bool Success { get; }
        public string Value { get; }
    }
}
