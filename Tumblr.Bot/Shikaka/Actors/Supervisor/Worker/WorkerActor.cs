using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Enums;
using Tumblr.Bot.Exceptions;
using Tumblr.Bot.OutgoingMessages;
using Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.Connector;
using Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.ConversationRetriever;
using Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.ConversationsRetriever;
using Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.GreetSessionHandler;
using Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.MessageSender;
using Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.SessionChecker;
using Tumblr.Bot.Shikaka.ChildActorContainers;
using Tumblr.Bot.Shikaka.Messages.ChatLogAppender;
using Tumblr.Bot.Shikaka.Messages.ConnectionPermitGranter;
using Tumblr.Bot.Shikaka.Messages.Connector;
using Tumblr.Bot.Shikaka.Messages.ConversationRetriever;
using Tumblr.Bot.Shikaka.Messages.ConversationsRetriever;
using Tumblr.Bot.Shikaka.Messages.GlobalStatsUpdater;
using Tumblr.Bot.Shikaka.Messages.GreetSessionHandler;
using Tumblr.Bot.Shikaka.Messages.MessageSender;
using Tumblr.Bot.Shikaka.Messages.SessionChecker;
using Tumblr.Bot.Shikaka.Messages.SQLite;
using Tumblr.Bot.Shikaka.Messages.Worker;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.Shikaka.StateContainers;
using Tumblr.Bot.SQLite;
using Tumblr.Bot.UserInterface;
using Tumblr.Waifu.Exceptions;
using Tumblr.Waifu.JsonObjects;
using Waifu.Collections;
using Waifu.Net.Http.Exceptions;
using Waifu.Sys;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.Worker
{
    internal class WorkerActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private static readonly object Writelock;

        static WorkerActor()
        {
            Writelock = new object();
        }

        private readonly WorkerPropsContainer _props;

        public WorkerActor(
            WorkerPropsContainer propsContainer)
        {
            _props = propsContainer;
            Become(Disconnected);
            if (_props.State.WorkerState != WorkerState.Disconnected)
                _props.State.WorkerState = WorkerState.Disconnected;
        }

        private void Disconnected()
        {
            Receive<StartConnectingMessage>(
                HandleStartConnectingMessage
            );
            Receive<SendMessageFailedMessage>(
                HandleSendMessageFailedMessage
            );
        }

        private void Connecting()
        {
            Receive<ConnectionPermitGrantedMessage>(
                HandleConnectionPermitGrantedMessage
            );
            Receive<ConnectionAttemptSucceededMessage>(
                HandleConnectionAttemptSucceededMessage
            );
        }

        private void Connected()
        {
            Receive<SessionOkMessage>(
                HandleSessionOkMessage
            );
            ReceiveAsync<ConversationsRetrievedMessage>(
                HandleConversationsRetrievedMessage
            );
            Receive<MessageSentSuccessfullyMessage>(
                HandleMessageSentSuccessfullyMessage
            );
        }

        private bool HandleStartConnectingMessage(
            StartConnectingMessage messageRcvd)
        {
            _props.UiDataGridItem.Status = "Waiting for permission to connect: ...";

            Become(Connecting);

            var requestConnectionPermitMsg = new RequestConnectionPermitMessage();
            _props.ConnectionPermitGranterActor.Tell(requestConnectionPermitMsg);
            return true;
        }

        private bool HandleConnectionPermitGrantedMessage(
            ConnectionPermitGrantedMessage messageRcvd)
        {
            _props.UiDataGridItem.Status = "Connecting: ...";

            _props.TumblrClient.HttpConfig.Proxy = _props.Collections.Proxies.GetNext();

            var attemptConnection = new AttemptToConnectMessage();
            _props.Children.ConnectorActor.Tell(attemptConnection);

            return true;
        }

        private bool HandleConnectionAttemptSucceededMessage(
            ConnectionAttemptSucceededMessage messageRcvd)
        {
            var relenquishPermit = new RelenquishConnectionPermitMessage();
            _props.ConnectionPermitGranterActor.Tell(
                relenquishPermit
            );

            _props.UiDataGridItem.Status = "Connected";
            _props.State.LoginErrors = 0;
            _props.State.WorkerState = WorkerState.Connected;
            Become(Connected);

            var readyToSendMsgsMsg = new ReadyToSendMessagesMessage();
            _props.Children.MessageSenderActor.Tell(
                readyToSendMsgsMsg
            );

            var startOrResumeGreetSessionMsg = new StartOrResumeGreetSessionMessage();
            _props.Children.GreetSessionHandlerActor.Tell(
                startOrResumeGreetSessionMsg
            );

            //var updateGlobalStatMsg = new UpdateGlobalStatMessage(
            //    StatKind.Online,
            //    IncrementOrDecrement.Increment,
            //    1
            //);
            //_props.GlobalStatsUpdaterActor.Tell(
            //    updateGlobalStatMsg
            //);

            if (!_props.WorkersOnline.Contains(_props.Index))
                _props.WorkersOnline.Add(_props.Index);

            ScheduleCheckSession();
            return true;
        }

        private bool HandleSessionOkMessage(
            SessionOkMessage messageRcvd)
        {
            ScheduleCheckSession();

            if (messageRcvd.UnreadMessagesCount <= 0)
            {
                if (!global::Waifu.Sys.Settings.Get<bool>(
                    Constants.SendNextLineToCachedConvos))
                {
                    return true;
                }

                if (!_props.State.FirstInboxLoad)
                    return true;
            }

            if (_props.State.WaitingForConversationsRetrieval)
                return true;

            if (global::Waifu.Sys.Settings.Get<bool>(Constants.DisableReplying))
                return true;

            _props.State.WaitingForConversationsRetrieval = true;
            var retrieveConvosMsg = new RetrieveConversationsMessage();
            _props.Children.ConversationsRetrieverActor.Tell(
                retrieveConvosMsg
            );

            return true;
        }

        private async Task<bool> HandleConversationsRetrievedMessage(
            ConversationsRetrievedMessage messageRcvd)
        {
            _props.State.WaitingForConversationsRetrieval = false;

            if (messageRcvd.Conversations.Count == 0)
            {
                if (_props.State.FirstInboxLoad)
                    _props.State.FirstInboxLoad = false;

                return true;
            }

            foreach (var convo in messageRcvd.Conversations)
            {
                if (convo == null)
                    continue;

                if (convo.Messages == null)
                    continue;

                if (convo.Messages.Data == null ||
                    convo.Messages.Data.Count == 0)
                {
                    continue;
                }

                if (!convo.CanSend)
                    continue;

                if (convo.UnreadMessagesCount <= 0)
                {
                    if (!global::Waifu.Sys.Settings.Get<bool>(
                        Constants.SendNextLineToCachedConvos))
                    {
                        continue;
                    }

                    if (!_props.State.FirstInboxLoad)
                        continue;
                }

                if (convo.Participants == null ||
                    convo.Participants.Count == 0)
                {
                    continue;
                }

                var convoId = convo.Id;
                if (string.IsNullOrWhiteSpace(convoId))
                    continue;

                var from =
                    convo.Participants.FirstOrDefault(
                        item => !string.Equals(
                            item.Name,
                            _props.TumblrAccount.Username,
                            StringComparison.CurrentCultureIgnoreCase
                        )
                    );

                if (from == null ||
                    string.IsNullOrWhiteSpace(from.Name))
                {
                    continue;
                }

                var fromUsername = from.Name;

                await HandleConvo(
                    fromUsername,
                    convoId,
                    convo
                ).ConfigureAwait(false);
            }

            if (_props.State.FirstInboxLoad)
                _props.State.FirstInboxLoad = false;

            return true;
        }

        private bool HandleMessageSentSuccessfullyMessage(
            MessageSentSuccessfullyMessage messageRcvd)
        {
            var flags = messageRcvd.OutgoingMessage.Flags;
            if (flags.HasFlag(OutgoingMessageFlags.Reply))
            {
                HandleOnReplySent(
                    messageRcvd.OutgoingMessage
                );
            }
            else if (flags.HasFlag(OutgoingMessageFlags.Greet))
            {
                HandleOnGreetSent(
                    messageRcvd.OutgoingMessage
                );
            }

            if (!flags.HasFlag(OutgoingMessageFlags.Link))
                return true;

            var updateGlobalStatMsg = new UpdateGlobalStatMessage(
                StatKind.Links,
                IncrementOrDecrement.Increment,
                1
            );
            _props.GlobalStatsUpdaterActor.Tell(updateGlobalStatMsg);

            _props.State.SendMessageErrors = 0;

            return true;
        }

        private static bool HandleSendMessageFailedMessage(
            SendMessageFailedMessage messageRcvd)
        {
            if (messageRcvd.OutgoingMessage.SendErrors < 3)
                return true;

            //var hasGreetFlag =
            //    messageRcvd.OutgoingMessage.Flags.HasFlag(
            //        OutgoingMessageFlags.Greet
            //    );

            //if (!hasGreetFlag &&
            //    messageRcvd.OutgoingMessage.ScriptWaifu != null)
            //{
            //    messageRcvd.OutgoingMessage.ScriptWaifu.Pending = false;
            //}

            return true;
        }

        private void HandleOnReplySent(
            OutgoingMessage outgoingMessage)
        {
            var convo = outgoingMessage.ScriptWaifu;
            if (convo == null)
            {
                throw new InvalidOperationException(
                    "ScriptWaifu property of MessageSentSuccessfullyMessage instance must not be null if it has OutgoingMessageFlags.Reply"
                );
            }

            _props.UiDataGridItem.OutCnt = ++_props.State.LocalStats.Out;

            var updateGlobalStatMsg = new UpdateGlobalStatMessage(
                StatKind.Out,
                IncrementOrDecrement.Increment,
                1
            );
            _props.GlobalStatsUpdaterActor.Tell(updateGlobalStatMsg);

            if (convo.IsComplete)
            {
                AddBlacklist(outgoingMessage.ToUsername);
                updateGlobalStatMsg = new UpdateGlobalStatMessage(
                    StatKind.Completed,
                    IncrementOrDecrement.Increment,
                    1
                );
                _props.GlobalStatsUpdaterActor.Tell(updateGlobalStatMsg);
            }

            var updateConvoEntMsg = new UpdateConvoEntityMessage(
                _props.TumblrAccount.Username,
                outgoingMessage,
                convo
            );
            _props.SQLiteActor.Tell(updateConvoEntMsg);

            var appendChatLogMsg = new AppendChatLogMessage(
                ChatLogMessageDirection.Outgoing,
                _props.TumblrAccount.Username,
                outgoingMessage.ToUsername,
                outgoingMessage.Body
            );
            _props.ChatLogAppenderActor.Tell(appendChatLogMsg);
        }

        private void HandleOnGreetSent(
            OutgoingMessage outgoingMessage)
        {
            var addGreetBlacklistMsg = new AddGreetBlacklistEntityMessage(
                outgoingMessage.ToUsername
            );
            _props.SQLiteActor.Tell(addGreetBlacklistMsg);

            var greetSentSuccessfully = new EnqueueNextGreetMessage();
            _props.Children.GreetSessionHandlerActor.Tell(
                greetSentSuccessfully
            );

            _props.UiDataGridItem.GreetCnt = ++_props.State.LocalStats.Greets;

            var updateGlobalStatMsg = new UpdateGlobalStatMessage(
                StatKind.Greets,
                IncrementOrDecrement.Increment,
                1
            );
            _props.GlobalStatsUpdaterActor.Tell(updateGlobalStatMsg);
        }

        private async Task HandleConvo(
            string fromUsername,
            string convoId,
            TumblrConversation convo)
        {
            foreach (var message in convo.Messages.Data)
            {
                if (message == null)
                    continue;

                if (string.IsNullOrWhiteSpace(message.Participant))
                    continue;

                if (message.Participant == _props.TumblrAccount.Uuid)
                    continue;

                if (await _props.SQLiteDb.ChatBlacklistTable.ContainsItemAsync(
                    fromUsername).ConfigureAwait(false))
                {
                    var retrConvoMsg = new RetrieveConversationMessage(
                        convoId,
                        message.Participant
                    );
                    _props.Children.ConversationRetrieverActor.Tell(retrConvoMsg);
                    return;
                }

                if (!await HandleIncomingMessage(
                    fromUsername,
                    convoId,
                    message
                ).ConfigureAwait(false))
                {
                    return;
                }
            }
        }

        private async Task<bool> HandleIncomingMessage(
            string fromUsername,
            string convoId,
            ConvoDatum message)
        {
            _props.State.LastMessageRcvdAt = DateTimeOffset.Now;

            UpdateInUiElements(fromUsername, message.Message);

            var fromUuid = message.Participant;

            var retrConvoMsg = new RetrieveConversationMessage(
                convoId,
                fromUuid
            );
            _props.Children.ConversationRetrieverActor.Tell(retrConvoMsg);

            var convos = _props.State.Convos;
            if (!convos.ContainsKey(fromUsername))
            {
                var script = new ReadOnlyCollection<string>(_props.Collections.Script);
                var waifu = new ScriptWaifu(script);
                convos.Add(fromUsername, waifu);

                var scriptLines = DbHelpers.ListAsString(script);
                var sha256Sum = await Task.Run(
                    () => DbHelpers.CalculateScriptKey(scriptLines)
                ).ConfigureAwait(false);

                var addNewConvoEntityMsg = new AddNewConvoEntityMessage(
                    _props.TumblrAccount.Username,
                    _props.TumblrAccount.Uuid,
                    fromUsername,
                    fromUuid,
                    convoId,
                    sha256Sum
                );
                _props.SQLiteActor.Tell(addNewConvoEntityMsg);

                var updateGlobalStatMsg = new UpdateGlobalStatMessage(
                    StatKind.Convos,
                    IncrementOrDecrement.Increment,
                    1
                );
                _props.GlobalStatsUpdaterActor.Tell(updateGlobalStatMsg);
            }

            var messageBody = message.Message;
            if (ScriptWaifu.HasRestrictedKeyword(
                messageBody,
                _props.Collections.Restricts))
            {
                var updateGlobalStatMsg = new UpdateGlobalStatMessage(
                    StatKind.Restricts,
                    IncrementOrDecrement.Increment,
                    1
                );
                _props.GlobalStatsUpdaterActor.Tell(updateGlobalStatMsg);

                AddBlacklist(fromUsername);
                return false;
            }

            var convo = _props.State.Convos[fromUsername];
            convo.LastMessageReceivedAt = DateTime.Now;

            if (convo.Pending)
                return false;

            convo.Pending = true;

            if (!convo.IsFirstLine)
            {
                if (convo.TryFindKeywordResponse(messageBody,
                    _props.Collections.Keywords,
                    out var keywordResponse))
                {
                    var outgoingMessage = CreateReplyMessage(
                        fromUuid,
                        fromUsername,
                        keywordResponse,
                        convo,
                        // ReSharper disable once ArgumentsStyleLiteral
                        isKeywordResponse: true
                    );

                    TellMessageSenderToScheduleMessage(
                        outgoingMessage
                    );
                    return true;
                }
            }

            var reply = convo.NextLine();

            if (string.IsNullOrWhiteSpace(reply))
            {
                convo.Pending = false;
                return false;
            }

            {
                var outgoingMessage = CreateReplyMessage(
                    fromUuid,
                    fromUsername,
                    reply,
                    convo
                );

                TellMessageSenderToScheduleMessage(
                    outgoingMessage
                );
            }

            return true;
        }

        private void TellMessageSenderToScheduleMessage(
            OutgoingMessage outgoingMessage)
        {
            var scheduleOutgoingMsgMsg = new ScheduleOutgoingMessageMessage(
                outgoingMessage
            );
            _props.Children.MessageSenderActor.Tell(
                scheduleOutgoingMsgMsg
            );
        }

        private OutgoingMessage CreateReplyMessage(
            string toUuid,
            string toUsername,
            string reply,
            ScriptWaifu waifu,
            bool isKeywordResponse = false)
        {
            var flags = OutgoingMessageFlags.Reply;
            if (reply.Contains("%s"))
            {
                reply = reply.Replace("%s", _props.Collections.Links.GetNext());
                flags |= OutgoingMessageFlags.Link;
            }

            if (isKeywordResponse)
                flags |= OutgoingMessageFlags.Keyword;

            var outgoingMessage = new OutgoingMessage(
                toUuid,
                _props.TumblrAccount.Uuid,
                toUsername,
                _props.TumblrAccount.Username,
                reply,
                waifu,
                flags
            );
            return outgoingMessage;
        }

        private void AddBlacklist(string item)
        {
            var addBlacklistEntityMsg = new AddChatBlacklistEntityMessage(
                item
            );
            _props.SQLiteActor.Tell(
                addBlacklistEntityMsg
            );
        }

        private void UpdateInUiElements(
            string fromUsername,
            string message)
        {
            _props.UiDataGridItem.InCnt = ++_props.State.LocalStats.In;

            var updateGlobalStatMsg = new UpdateGlobalStatMessage(
                StatKind.In,
                IncrementOrDecrement.Increment,
                1
            );
            _props.GlobalStatsUpdaterActor.Tell(updateGlobalStatMsg);

            var appendChatLogMsg = new AppendChatLogMessage(
                ChatLogMessageDirection.Incoming,
                _props.TumblrAccount.Username,
                fromUsername,
                message
            );
            _props.ChatLogAppenderActor.Tell(appendChatLogMsg);
        }

        private void ScheduleCheckSession()
        {
#if DEBUG
            var seconds = ThreadSafeStaticRandom.RandomInt(8, 14);
#else
            var seconds = ThreadSafeStaticRandom.RandomInt(35, 45);
#endif
            var timespan = TimeSpan.FromSeconds(seconds);

            var checkSessionMsg = new AttemptCheckSessionMessage();

            _props.State.SessionCheckerJob = Context.System.Scheduler.ScheduleTellOnceCancelable(
                timespan,
                _props.Children.SessionCheckerActor,
                checkSessionMsg,
                Self
            );
        }

        protected override void PreStart()
        {
            var connectorPropsContainer = new ConnectorPropsContainer(
                _props.TumblrClient
            );
            var props = ConnectorActor.CreateProps(
                connectorPropsContainer
            );
            var connector = Context.ActorOf(props, "connector");

            var sessionCheckerPropsContainer = new SessionCheckerPropsContainer(
                _props.TumblrClient
            );
            props = SessionCheckerActor.CreateProps(
                sessionCheckerPropsContainer
            );
            var sessionChecker = Context.ActorOf(props, "sessionchecker");

            var convoRsetrieverStateCont = new ConversationsRetrieverStateContainer();
            var convosRetPropsCont = new ConversationsRetrieverPropsContainer(
                _props.TumblrClient,
                convoRsetrieverStateCont
            );
            props = ConversationsRetrieverActor.CreateProps(convosRetPropsCont);
            var convosRetriever = Context.ActorOf(props, "conversationsretriever");

            var maxConcurrentReplies =
                global::Waifu.Sys.Settings.Get<int>(Constants.MaxConcurrentReplies);
            var msgSenderStateContainer = new MessageSenderStateContainer();
            var mgSndrPropsContainer = new MessageSenderPropsContainer(
                maxConcurrentReplies,
                _props.TumblrClient,
                msgSenderStateContainer
            );
            props = MessageSenderActor.CreateProps(mgSndrPropsContainer);
            var msgSender = Context.ActorOf(props, "messagesender");

            var greetStateContainer = new GreetSessionHandlerStateContainer();
            var greetPropsContainer = new GreetSessionHandlerPropsContainer(
                _props.Collections.Greets,
                _props.Collections.Links,
                _props.TumblrAccount,
                greetStateContainer,
                _props.ContactReaderActor,
                msgSender
            );
            props = GreetSessionHandlerActor.CreateProps(
                greetPropsContainer
            );
            var greetSessionHandlerActor = Context.ActorOf(props, "greetsessionhandler");

            var convoRetrieverPropsCont = new ConversationRetrieverPropsContainer(
                _props.TumblrClient
            );
            props = ConversationRetrieverActor.CreateProps(
                convoRetrieverPropsCont
            );
            var convoRetrieverActor = Context.ActorOf(props, "conversationretriever");

            _props.Children = new WorkerChildActorsContainer(
                connector,
                sessionChecker,
                convosRetriever,
                msgSender,
                greetSessionHandlerActor,
                convoRetrieverActor
            );
        }

        protected override void PostRestart(Exception reason)
        {
        }

        protected override void PreRestart(Exception reason, object message)
        {
            _props.State.SessionCheckerJob.CancelIfNotNull();
            _props.State.SessionCheckerJob = null;

            _props.State.WaitingForConversationsRetrieval = false;

            _props.UiDataGridItem.Status =
                $"[{_props.State.LoginErrors}] {reason.GetType().Name}: {reason.Message} - Will reconnect in ~45 seconds.";

            if (_props.WorkersOnline.Contains(_props.Index))
            {
                _props.WorkersOnline.Remove(_props.Index);
            }

            PostStop();
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(HandleException);
        }

        private Directive HandleException(
            Exception ex)
        {
            switch (ex)
            {
                case DeadProxyException _:
                    {
                        var relenquishPermit = new RelenquishConnectionPermitMessage();
                        _props.ConnectionPermitGranterActor.Tell(relenquishPermit);
                        return Directive.Escalate;
                    }

                case SessionCheckFailedException e when (e.InnerException is HttpRequestFailedException e2) && e2.HttpStatusCode == System.Net.HttpStatusCode.Forbidden:

                    _props.UiDataGridItem.Status = "account forbidden from using messaging api.";
                    Context.Stop(Self);
                    lock (Writelock) {
                        using (var writer = new StreamWriter("sessions_forbidden.txt", append: true)) {
                            writer.WriteLine(_props.TumblrAccount.ToString());
                        }
                        
                    }
                    return Directive.Stop;

                case ConnectionFailedException _:
                    {
                        var relenquishPermit = new RelenquishConnectionPermitMessage();
                        _props.ConnectionPermitGranterActor.Tell(relenquishPermit);

                        if (ex.InnerException is TumblrSessionNotAuthorizedException)
                        {
                            Context.Stop(Self);
                            _props.UiDataGridItem.Status = "Session not authorized";

                            lock (Writelock)
                            {
                                using (var sw = new StreamWriter("sessions_unauthorized.txt", true))
                                {
                                    sw.WriteLine(_props.TumblrAccount.ToString());
                                }
                            }

                            return Directive.Stop;
                        }

                        _props.State.LoginErrors++;

                        if (_props.State.LoginErrors < global::Waifu.Sys.Settings.Get<int>(
                            Constants.MaxLoginErrors))
                        {
                            return Directive.Escalate;
                        }

                        _props.UiDataGridItem.Status = "Too many login errors.";
                        Context.Stop(Self);
                        return Directive.Stop;
                    }

                default:
                    return Directive.Escalate;
            }
        }

        public static Props CreateProps(
            WorkerPropsContainer propsContainer)
        {
            return Props.Create(
                () => new WorkerActor(propsContainer)
            );
        }
    }
}