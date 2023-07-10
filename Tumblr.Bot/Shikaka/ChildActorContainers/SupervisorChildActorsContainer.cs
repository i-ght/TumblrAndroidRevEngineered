using System.Collections.Generic;
using Akka.Actor;

namespace Tumblr.Bot.Shikaka.ChildActorContainers
{
    internal class SupervisorChildActorsContainer
    {
        public SupervisorChildActorsContainer(
            IActorRef exceptionLoggerActor,
            IActorRef globalStatsUpdaterActor,
            IActorRef connectionPermitGranterActor,
            IActorRef chatLogAppenderActor,
            IActorRef contactReaderActor,
            IReadOnlyCollection<IActorRef> workerActors)
        {
            ExceptionLoggerActor = exceptionLoggerActor;
            GlobalStatsUpdaterActor = globalStatsUpdaterActor;
            ConnectionPermitGranterActor = connectionPermitGranterActor;
            ChatLogAppenderActor = chatLogAppenderActor;
            ContactReaderActor = contactReaderActor;
            WorkerActors = workerActors;
        }

        public IActorRef ExceptionLoggerActor { get; }
        public IActorRef GlobalStatsUpdaterActor { get; }
        public IActorRef ConnectionPermitGranterActor { get; }
        public IActorRef ChatLogAppenderActor { get; }
        public IActorRef ContactReaderActor { get; }
        public IReadOnlyCollection<IActorRef> WorkerActors { get; }
    }
}
