using Akka.Actor;

namespace Tumblr.Bot.Shikaka.ChildActorContainers
{
    internal class WorkerChildActorsContainer
    {
        public WorkerChildActorsContainer(
            IActorRef connectorActor,
            IActorRef sessionCheckerActor,
            IActorRef conversationsRetrieverActor,
            IActorRef messageSenderActor,
            IActorRef greetSessionHandlerActor,
            IActorRef conversationRetrieverActor)
        {
            ConnectorActor = connectorActor;
            SessionCheckerActor = sessionCheckerActor;
            ConversationsRetrieverActor = conversationsRetrieverActor;
            MessageSenderActor = messageSenderActor;
            GreetSessionHandlerActor = greetSessionHandlerActor;
            ConversationRetrieverActor = conversationRetrieverActor;
        }

        public IActorRef ConnectorActor { get; set; }
        public IActorRef SessionCheckerActor { get; set; }
        public IActorRef ConversationsRetrieverActor { get; set; }
        public IActorRef MessageSenderActor { get; }
        public IActorRef GreetSessionHandlerActor { get; }
        public IActorRef ConversationRetrieverActor { get; }
    }
}
