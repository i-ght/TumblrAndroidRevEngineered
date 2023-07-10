using System;
using Akka.Actor;
using Tumblr.Bot.OutgoingMessages;
using Tumblr.Bot.Shikaka.Messages.ContactReader;
using Tumblr.Bot.Shikaka.Messages.GreetSessionHandler;
using Tumblr.Bot.Shikaka.Messages.MessageSender;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.UserInterface;
using Waifu.Collections;
using Waifu.Sys;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.GreetSessionHandler
{
    internal class GreetSessionHandlerActor : ReceiveActor
    {
        private readonly GreetSessionHandlerPropsContainer _props;

        public GreetSessionHandlerActor(
            GreetSessionHandlerPropsContainer propsContainer)
        {
            _props = propsContainer;
            Become(NotReady);
        }

        private void NotReady()
        {
            Receive<StartOrResumeGreetSessionMessage>(
                HandleStartOrResumeGreetSessionMessage
            );
        }

        private void Ready()
        {
            Receive<ContactMessage>(
                HandleContactMessage
            );
            Receive<TellMessageSenderSendMessageMessage>(
                HandleEnqueueGreetMessage
            );
            Receive<EnqueueNextGreetMessage>(
                HandleEnqueueNextGreetMessage
            );
        }

        private bool HandleStartOrResumeGreetSessionMessage(
            StartOrResumeGreetSessionMessage messageRcvd)
        {
            Become(Ready);

            GetNextGreetFromContactReaderIfAllowed();

            //if (!string.IsNullOrWhiteSpace(_props.State.Contact) &&
            //    _props.State.TotalGreetsEnqueued <=
            //    global::Waifu.Sys.Settings.Get<int>(Constants.MaxTotalGreets))
            //{
            //    var split = _props.State.Contact.Split('|');
            //    var username = split[0];
            //    var uuid = split[1];

            //    var contactMsg = new ContactMessage(
            //        username,
            //        uuid
            //    );
            //    Self.Tell(contactMsg);
            //}
            //else
            //{
            //    GetNextGreetFromContactReaderIfAllowed();
            //}

            return true;
        }

        private bool HandleContactMessage(
            ContactMessage messageRcvd)
        {
            if (string.IsNullOrWhiteSpace(messageRcvd.Username))
            {
                ScheduleGetNextContact();
                return true;
            }

            //_props.State.Contact = messageRcvd.ToString();

            var greetMsgBody = _props.Greets.GetNext();
            if (string.IsNullOrWhiteSpace(greetMsgBody))
            {
                ScheduleGetNextContact();
                return true;
            }

            greetMsgBody = ScriptWaifu.Spin(greetMsgBody);

            ScheduleTellMessageSenderSendMessageMessage(
                messageRcvd.Uuid,
                messageRcvd.Username,
                greetMsgBody
            );

            return true;
        }

        private bool HandleEnqueueGreetMessage(
            TellMessageSenderSendMessageMessage messageRcvd)
        {
            var flags = OutgoingMessageFlags.Greet;
            string messageBody;
            if (messageRcvd.MessageBody.Contains("%s"))
            {
                flags |= OutgoingMessageFlags.Link;
                messageBody = messageRcvd.MessageBody.Replace(
                    "%s",
                    _props.Links.GetNext()
                );
            }
            else
            {
                messageBody = messageRcvd.MessageBody;
            }

            var outgoingMessage = new OutgoingMessage(
                messageRcvd.ContactUuid,
                _props.TumblrAccount.Uuid,
                messageRcvd.ContactUsername,
                _props.TumblrAccount.Username,
                messageBody,
                flags
            );

            var scheduleOutgoingMsgMsg = new ScheduleOutgoingMessageMessage(
                outgoingMessage
            );
            _props.MessageSenderActor.Tell(
                scheduleOutgoingMsgMsg
            );

            //_props.State.Contact = string.Empty;

            return true;
        }

        private bool HandleEnqueueNextGreetMessage(
            EnqueueNextGreetMessage messageRcvd)
        {
            GetNextGreetFromContactReaderIfAllowed();
            return true;
        }

        private void GetNextGreetFromContactReaderIfAllowed()
        {
            if (_props.State.TotalGreetsEnqueued <=
                global::Waifu.Sys.Settings.Get<int>(Constants.MaxTotalGreets))
            {
                var getContactMsg = new GetNextContactMessage();
                _props.ContactReaderActor.Tell(getContactMsg);
            }
            else
            {
                var timespan = TimeSpan.FromSeconds(30);
                var msg = new EnqueueNextGreetMessage();
                _props.State.PendingGreetSessionCheck = Context.System.Scheduler.ScheduleTellOnceCancelable(
                    timespan,
                    Self,
                    msg,
                    Self
                );
            }
        }

        private void ScheduleGetNextContact()
        {
            var timespan = TimeSpan.FromSeconds(30);
            var getContactMsg = new GetNextContactMessage();
            _props.State.PendingGetContactJob = Context.System.Scheduler.ScheduleTellOnceCancelable(
                timespan,
                _props.ContactReaderActor,
                getContactMsg,
                Self
            );
        }

        private void ScheduleTellMessageSenderSendMessageMessage(
            string toUuid,
            string toUsername,
            string messageBody)
        {

            TimeSpan timespan;
            if (_props.State.GreetsEnqueuedThisSession++ < _props.State.MaxGreetsToEnqueueThisSession)
            {
                var minSeconds = global::Waifu.Sys.Settings.Get<int>(
                    Constants.MinGreetDelay
                );
                var maxSeconds = global::Waifu.Sys.Settings.Get<int>(
                    Constants.MaxGreetDelay
                );

                if (minSeconds > maxSeconds)
                    minSeconds = maxSeconds;

                var seconds = ThreadSafeStaticRandom.RandomInt(
                    minSeconds,
                    maxSeconds
                );
                timespan = TimeSpan.FromSeconds(seconds);
            }
            else
            {
                var minGreets = global::Waifu.Sys.Settings.Get<int>(
                    Constants.MinGreetsPerSession
                );
                var maxGreets = global::Waifu.Sys.Settings.Get<int>(
                    Constants.MaxGreetsPerSession
                );

                if (minGreets > maxGreets)
                    minGreets = maxGreets;

                _props.State.MaxGreetsToEnqueueThisSession = ThreadSafeStaticRandom.RandomInt(
                    minGreets,
                    maxGreets
                );
                _props.State.GreetsEnqueuedThisSession = 0;

                var minMinutes = global::Waifu.Sys.Settings.Get<int>(
                    Constants.MinGreetSessionDelay
                );
                var maxMinutes = global::Waifu.Sys.Settings.Get<int>(
                    Constants.MaxGreetSessionDelay
                );

                if (minMinutes > maxMinutes)
                    minMinutes = maxMinutes;
 
                var minutes = ThreadSafeStaticRandom.RandomInt(
                    minMinutes,
                    maxMinutes
                );
                timespan = TimeSpan.FromMinutes(minutes);
            }

            var enqueueGreetMsg = new TellMessageSenderSendMessageMessage(
                toUsername,
                toUuid,
                messageBody
            );
            _props.State.PendingGreetJob = Context.System.Scheduler.ScheduleTellOnceCancelable(
                timespan,
                Self,
                enqueueGreetMsg,
                Self
            );

            _props.State.TotalGreetsEnqueued++;
        }

        protected override void PreRestart(Exception reason, object message)
        {
            _props.State.PendingGreetJob.CancelIfNotNull();
            _props.State.PendingGreetJob = null;

            _props.State.PendingGetContactJob.CancelIfNotNull();
            _props.State.PendingGetContactJob = null;

            _props.State.PendingGreetSessionCheck.CancelIfNotNull();
            _props.State.PendingGreetSessionCheck = null;

            base.PreRestart(reason, message);
        }

        public static Props CreateProps(
            GreetSessionHandlerPropsContainer propsContainer)
        {
            return Props.Create(
                () => new GreetSessionHandlerActor(propsContainer)
            );
        }

    }
}
