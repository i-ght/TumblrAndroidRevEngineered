using System;

namespace Tumblr.Bot.OutgoingMessages
{
    [Flags]
    internal enum OutgoingMessageFlags
    {
        None = 0,
        Reply = 2,
        Greet = 4,
        Link = 8,
        Keyword = 16
    }
}
