using System;
using System.Collections.Generic;
using Akka.Actor;
using Tumblr.Bot.Shikaka.Actors.Supervisor.ChatLogAppender;
using Tumblr.Bot.Shikaka.Actors.Supervisor.ConnectionPermitGranter;
using Tumblr.Bot.Shikaka.Actors.Supervisor.ContactReader;
using Tumblr.Bot.Shikaka.Actors.Supervisor.ExceptionLogger;
using Tumblr.Bot.Shikaka.Actors.Supervisor.GlobalStatsUpdater;
using Tumblr.Bot.Shikaka.Actors.Supervisor.SQLite;
using Tumblr.Bot.Shikaka.Actors.Supervisor.Worker;
using Tumblr.Bot.Shikaka.ChildActorContainers;
using Tumblr.Bot.Shikaka.Messages.ContactReader;
using Tumblr.Bot.Shikaka.Messages.ExceptionLogger;
using Tumblr.Bot.Shikaka.Messages.Supervisor;
using Tumblr.Bot.Shikaka.Messages.Worker;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.Shikaka.StateContainers;
using Tumblr.Bot.UserInterface;
using Tumblr.Waifu;
using Waifu.Collections;
using Waifu.Net.Http;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor
{
    internal class SupervisorActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly SupervisorPropsContainer _props;

        public SupervisorActor(SupervisorPropsContainer propsContainer)
        {
            _props = propsContainer;
            Receive<StartAppSessionMessage>(
                HandleStartAppSessionMessage
            );
            Receive<TellContactReaderAdvanceContactStreamReaderToAnItemNotBlacklistedMessage>(
                HandleTellContactReaderAdvanceContactStreamReaderToAnItemNotBlacklistedMessage
            );
        }

        #region Message handlers

        private bool HandleStartAppSessionMessage(
            StartAppSessionMessage messageRcvd)
        {
            foreach (var worker in _props.Children.WorkerActors)
            {
                var startConnectingMessage = new StartConnectingMessage();
                worker.Tell(startConnectingMessage);
            }

            return true;
        }

        private bool HandleTellContactReaderAdvanceContactStreamReaderToAnItemNotBlacklistedMessage(
            TellContactReaderAdvanceContactStreamReaderToAnItemNotBlacklistedMessage messageRcvd)
        {
            var msg = new AdvanceContactStreamReaderToAnItemNotBlacklistedMessage();
            _props.Children.ContactReaderActor.Tell(msg);
            return true;
        }


        #endregion

        #region Actor lifecycle hooks

        protected override void PreStart()
        {
            var props = ExceptionLoggerActor.CreateProps();
            var exLoggerActor = Context.ActorOf(props, "exceptionlogger");

            var globalStatsState = new GlobalStatsUpdaterStateContainer(
                _props.GlobalStats
            );
            var globalStatsProps = new GlobalStatsUpdaterPropsContainer(
                globalStatsState
            );
            props = GlobalStatsUpdaterActor.CreateProps(
                globalStatsProps
            );
            var globalStatsUpdaterActor = Context.ActorOf(
                props,
                "globalstatsupdater"
            );

            var maxConn = global::Waifu.Sys.Settings.Get<int>(
                Constants.MaxConcurrentConnectionAttempts
            );
            var connectionPermitGranterStateContainer = new ConnectionPermitGranterStateContainer(
                maxConn
            );
            var connectionPermitPropsContainer = new ConnectionPermintGranterPropsContainer(
                maxConn,
                connectionPermitGranterStateContainer
            );
            props = ConnectionPermitGranterActor.CreateProps(connectionPermitPropsContainer);
            var connectionPermitGranterActor = Context.ActorOf(
                props,
                "connectionpermitgranter"
            );

            var sqlitePropsContainer = new SQLitePropsContainer(
                _props.SQLiteDatabase
            );
            props = SQLiteActor.CreateProps(sqlitePropsContainer);
            var sqliteActor = Context.ActorOf(props, "sqlite");

            var chatLogPropsContainer = new ChatLogAppenderPropsContainer(
                _props.ChatLogTextBox
            );
            props = ChatLogAppenderActor.CreateProps(chatLogPropsContainer);
            var chatLogAppenderActor = Context.ActorOf(props, "chatlogappender");

            var contactReaderPropsContainer =
                new ContactReaderPropsContainer(
                    _props.Collections.Contacts,
                    _props.SQLiteDatabase
                );
            props = ContactReaderActor.CreateProps(contactReaderPropsContainer);
            var contactReaderActor = Context.ActorOf(props, "contactreader");

            var workerActors = new List<IActorRef>();
            for (var i = 0; i < _props.MaxWorkers; i++)
            {
                var httpWaifuCfg = new HttpWaifuConfig();
                var client = new HttpWaifu(httpWaifuCfg);
                var account = _props.Collections.Accounts.GetNext(false);
                var tumblrClient = new TumblrClient(account, client);

                var localStats = new LocalStats();
                var workerStateContainer = new WorkerStateContainer(
                    localStats,
                    _props.Convos[account.Username]
                );

                var dGridItem = _props.DataGridUiControlSourceCollection[i];
                dGridItem.Account = account.Username;

                var workerPropsContainer = new WorkerPropsContainer(
                    i,
                    _props.Collections,
                    account,
                    tumblrClient,
                    workerStateContainer,
                    dGridItem,
                    _props.SQLiteDatabase,
                    _props.WorkersOnline,
                    connectionPermitGranterActor,
                    globalStatsUpdaterActor,
                    sqliteActor,
                    chatLogAppenderActor,
                    contactReaderActor
                );
                props = WorkerActor.CreateProps(workerPropsContainer);
                var workerActor = Context.ActorOf(props, $"worker{i}");

                workerActors.Add(workerActor);
            }

            _props.Children = new SupervisorChildActorsContainer(
                exLoggerActor,
                globalStatsUpdaterActor,
                connectionPermitGranterActor,
                chatLogAppenderActor,
                contactReaderActor,
                workerActors.AsReadOnly()
            );
        }

        protected override void PostRestart(Exception reason)
        {
        }

        protected override void PreRestart(Exception reason, object message)
        {
            PostStop();
        }

        #endregion

        #region Supervision

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(HandleException);
        }

        private Directive HandleException(Exception ex)
        {
            var logExceptionMessage = new LogExceptionMessage(ex);
            _props.Children.ExceptionLoggerActor.Tell(logExceptionMessage);

            switch (Sender.Path.Name)
            {
                case var _ when Sender.Path.Name.StartsWith("worker"):
                {
                    switch (ex)
                    {
                        default:
                        {
                            var startConnectingMessage = new StartConnectingMessage();
                            Context.System.Scheduler.ScheduleTellOnce(
                                TimeSpan.FromSeconds(45),
                                Sender,
                                startConnectingMessage,
                                Self
                            );

                            return Directive.Restart;
                        }
                    }
                }
            }

            return Directive.Restart;
        }

        #endregion

        public static Props CreateProps(
            SupervisorPropsContainer props)
        {
            return Props.Create(
                () => new SupervisorActor(props)
            );
        }
    }
}
