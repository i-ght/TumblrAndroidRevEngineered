using System.Collections.Generic;
using Akka.Actor;
using Tumblr.Bot.Shikaka.ChildActorContainers;
using Tumblr.Bot.Shikaka.StateContainers;
using Tumblr.Bot.SQLite;
using Tumblr.Bot.UserInterface;
using Tumblr.Waifu;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class WorkerPropsContainer
    {
        public WorkerPropsContainer(
            int index,
            Collections collections,
            TumblrAccount tumblrAccount,
            TumblrClient tumblrClient,
            WorkerStateContainer state,
            DataGridItem uiDataGridItem,
            SQLiteDb sqliteDb,
            List<int> workersOnline,
            IActorRef connectionPermitGranterActor,
            IActorRef globalStatsUpdaterActor,
            IActorRef sqliteActor,
            IActorRef chatLogAppenderActor,
            IActorRef contactReaderActor)
        {
            Index = index;
            Collections = collections;
            TumblrAccount = tumblrAccount;
            TumblrClient = tumblrClient;
            State = state;
            UiDataGridItem = uiDataGridItem;
            SQLiteDb = sqliteDb;
            WorkersOnline = workersOnline;
            ConnectionPermitGranterActor = connectionPermitGranterActor;
            GlobalStatsUpdaterActor = globalStatsUpdaterActor;
            SQLiteActor = sqliteActor;
            ChatLogAppenderActor = chatLogAppenderActor;
            ContactReaderActor = contactReaderActor;
        }

        public int Index { get; }
        public Collections Collections { get; }
        public TumblrAccount TumblrAccount { get; }
        public TumblrClient TumblrClient { get; }
        public WorkerStateContainer State { get; }
        public DataGridItem UiDataGridItem { get; }
        public SQLiteDb SQLiteDb { get; }
        public List<int> WorkersOnline { get; }
        public IActorRef ConnectionPermitGranterActor { get; }
        public IActorRef GlobalStatsUpdaterActor { get; }
        public IActorRef SQLiteActor { get; }
        public IActorRef ChatLogAppenderActor { get; }
        public IActorRef ContactReaderActor { get; }
        public WorkerChildActorsContainer Children { get; set; }
    }
}
