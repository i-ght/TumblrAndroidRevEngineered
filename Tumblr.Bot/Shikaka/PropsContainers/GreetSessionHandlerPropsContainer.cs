using System.Collections.Concurrent;
using Akka.Actor;
using Tumblr.Bot.Shikaka.StateContainers;
using Tumblr.Waifu;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class GreetSessionHandlerPropsContainer
    {
        public GreetSessionHandlerPropsContainer(
            ConcurrentQueue<string> greets,
            ConcurrentQueue<string> links,
            TumblrAccount tumblrAccount,
            GreetSessionHandlerStateContainer state,
            IActorRef contatReaderActor,
            IActorRef messageSenderActor)
        {
            Greets = greets;
            Links = links;
            TumblrAccount = tumblrAccount;
            State = state;
            ContactReaderActor = contatReaderActor;
            MessageSenderActor = messageSenderActor;
        }

        public ConcurrentQueue<string> Greets { get; }
        public ConcurrentQueue<string> Links { get; }
        public TumblrAccount TumblrAccount { get; }
        public GreetSessionHandlerStateContainer State { get; }
        public IActorRef ContactReaderActor { get; }
        public IActorRef MessageSenderActor { get; }
    }
}
