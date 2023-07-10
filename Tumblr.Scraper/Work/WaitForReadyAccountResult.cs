using Tumblr.Waifu;

namespace Tumblr.Scraper.Work
{
    internal class WaitForReadyAccountResult
    {
        public WaitForReadyAccountResult(
            bool result)
        {
            Result = result;
        }

        public WaitForReadyAccountResult(
            bool result,
            TumblrAccount value)
        : this(result)
        {
            Value = value;
        }

        public bool Result { get; }
        public TumblrAccount Value { get; }
    }
}
