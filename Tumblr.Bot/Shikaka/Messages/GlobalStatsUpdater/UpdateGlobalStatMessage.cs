using Tumblr.Bot.Enums;

namespace Tumblr.Bot.Shikaka.Messages.GlobalStatsUpdater
{
    internal class UpdateGlobalStatMessage
    {
        public UpdateGlobalStatMessage(
            StatKind statKind,
            IncrementOrDecrement incrementOrDecrement,
            int amount)
        {
            StatKind = statKind;
            IncrementOrDecrement = incrementOrDecrement;
            Amount = amount;
        }

        public StatKind StatKind { get; }
        public IncrementOrDecrement IncrementOrDecrement { get; }
        public int Amount { get; }

    }
}
