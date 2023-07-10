using Akka.Actor;
using Tumblr.Bot.UserInterface;
using Waifu.Sys;
using Settings = Waifu.Sys.Settings;

namespace Tumblr.Bot.Shikaka.StateContainers
{
    internal class GreetSessionHandlerStateContainer
    {
        public GreetSessionHandlerStateContainer()
        {
            var minGreets = Settings.Get<int>(
                Constants.MinGreetsPerSession
            );
            var maxGreets = Settings.Get<int>(
                Constants.MaxGreetsPerSession
            );

            if (minGreets > maxGreets)
            {
                minGreets = maxGreets;
            }

            MaxGreetsToEnqueueThisSession = ThreadSafeStaticRandom.RandomInt(
                minGreets,
                maxGreets
            );
        }

        public int TotalGreetsEnqueued { get; set; }
        public int GreetsEnqueuedThisSession { get; set; }
        public int MaxGreetsToEnqueueThisSession { get; set; }
        public ICancelable PendingGreetJob { get; set; }
        public ICancelable PendingGetContactJob { get; set; }
        public ICancelable PendingGreetSessionCheck { get; set; }
        //public string Contact { get; set; }
    }
}
