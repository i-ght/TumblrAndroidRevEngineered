using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Enums;
using Tumblr.Bot.Exceptions;
using Tumblr.Bot.OutgoingMessages;
using Tumblr.Bot.Shikaka.Messages.MessageSender;
using Tumblr.Bot.Shikaka.Messages.Worker;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.UserInterface;
using Waifu.Sys;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.MessageSender
{
    internal class MessageSenderActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly MessageSenderPropsContainer _props;

        /// <inheritdoc />
        /// <summary>
        /// Creates a new instance of a <see cref="T:Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.MessageSender.MessageSenderActor" />.
        /// </summary>
        /// <param name="propsContainer"></param>
        public MessageSenderActor(
            MessageSenderPropsContainer propsContainer)
        {
            _props = propsContainer;
            BecomeNotReady();
        }

        #region Actor behaviors

        /// <summary>
        /// Actor behavior that is set when the actor is started.  When the parent actor's account is in a logged out state, this is the behavior that this actor should be.
        /// </summary>
        private void NotReady()
        {
            Receive<ReadyToSendMessagesMessage>(
                HandleReadyToSendMessagesMessage
            );
        }

        private void BecomeNotReady()
        {
            _props.State.BehaviorState = MessageSenderActorBehaviorState.NotReady;
            Become(NotReady);
        }

        /// <summary>
        /// Behavior the actor becomes when the parent actor enters a logged in state and sends the <see cref="ReadyToSendMessagesMessage"/> indiciating that this actor should start sending messages.
        /// </summary>
        private void ReadyToWork()
        {
            Receive<ScheduleOutgoingMessageMessage>(
                HandleScheduleOutgoingMessageMessageWhenIdle
            );
            ReceiveAsync<SendOutgoingMessageMessage>(
                HandleSendOutgoingMessageMessage
            );
        }

        private void BecomeReadyToWork()
        {
            _props.State.BehaviorState = MessageSenderActorBehaviorState.ReadyToWork;
            Become(ReadyToWork);
        }

        /// <summary>
        /// Behavior the actor becomes when this <see cref="MessageSenderActor"/> is in the process of sending message(s) equal to the MaxConcurrentReplies property of this instances <see cref="MessageSenderPropsContainer"/> field.
        /// </summary>
        private void Working()
        {
            Receive<ScheduleOutgoingMessageMessage>(
                HandleScheduleOutgoingMessageMessageWhenWorkingOrNotReady
            );
            ReceiveAsync<SendOutgoingMessageMessage>(
                HandleSendOutgoingMessageMessage
            );
        }

        private void BecomeWorking()
        {
            _props.State.BehaviorState = MessageSenderActorBehaviorState.Working;
            Become(Working);
        }

        #endregion

        #region Message handlers

        /// <summary>
        /// Message handler for a <see cref="ReadyToSendMessagesMessage"/> message.
        /// This message is received from a parent actor instance of a <see cref="WorkerActor"/>.
        /// The actor sets its behavior to <see cref="ReadyToWork"/> and than checks the 
        /// <see cref="Queue{OutgoingMessage}"/>s in the actors state. If there are pending messages 
        /// in any of the queues, the messages are scheduled to be sent.
        /// </summary>
        /// <param name="messageRcvd"></param>
        /// <returns></returns>
        private bool HandleReadyToSendMessagesMessage(
            ReadyToSendMessagesMessage messageRcvd)
        {
            if (_props.State.BehaviorState !=
                MessageSenderActorBehaviorState.ReadyToWork)
            {
                BecomeReadyToWork();
            }

            SchedulePendingMessages();
            return true;
        }

        /// <summary>
        /// Message handler for a <see cref="ReadyToSendMessagesMessage"/> message.
        /// This message is received from a parent actor instance of a <see cref="WorkerActor"/>.
        /// Increments the pending messages property.  If it is equal to the MaxConcurrentReplies, sets 
        /// the actors behavior to <see cref="Working"/>. 
        /// </summary>
        /// <param name="messageRcvd"></param>
        /// <returns></returns>
        private bool HandleScheduleOutgoingMessageMessageWhenIdle(
            ScheduleOutgoingMessageMessage messageRcvd)
        {
            ScheduleSendMessageMessage(
                Context.System.Scheduler,
                Self,
                messageRcvd.OutgoingMessage
            );

            return true;
        }

        /// <summary>
        /// Message handler for a <see cref="ReadyToSendMessagesMessage"/> message.
        /// This message is received from a parent actor instance of a <see cref="WorkerActor"/>.
        /// In this handler, messages are added to a queue to be processed 
        /// later instead of right away.  This is used as a self rate 
        /// limiting mechanism.
        /// </summary>
        /// <param name="messageRcvd"></param>
        /// <returns></returns>
        private bool HandleScheduleOutgoingMessageMessageWhenWorkingOrNotReady(
            ScheduleOutgoingMessageMessage messageRcvd)
        {
            EnqueueOutgoingMessage(
                messageRcvd.OutgoingMessage
            );
            return true;
        }

        /// <summary>
        /// Message handler for a <see cref="SendOutgoingMessageMessage"/> message.
        /// The actor receives this message from itself and than attempts to send a message.
        /// </summary>
        /// <param name="messageRcvd"></param>
        /// <returns></returns>
        private async Task<bool> HandleSendOutgoingMessageMessage(
            SendOutgoingMessageMessage messageRcvd)
        {
            try
            {
                var pendingMsgToRemove =
                    _props.State.ScheduledMessages.FirstOrDefault(
                        item => item.Message.OutgoingMessage == messageRcvd.OutgoingMessage
                    );

                if (pendingMsgToRemove != null)
                    _props.State.ScheduledMessages.Remove(pendingMsgToRemove);

                var parent = Context.Parent;
                var scheduler = Context.System.Scheduler;
                var self = Self;

                await _props.Client.CreateMessage(
                    messageRcvd.OutgoingMessage.ToUuid,
                    messageRcvd.OutgoingMessage.Body
                );
                
                if (messageRcvd.OutgoingMessage.ScriptWaifu != null)
                    messageRcvd.OutgoingMessage.ScriptWaifu.Pending = false;

                HandlePendingMessagesStateAfterMessageSent(
                    scheduler,
                    self
                );

                var msgSentSuccessfullyMsg = new MessageSentSuccessfullyMessage(
                    messageRcvd.OutgoingMessage
                );
                parent.Tell(msgSentSuccessfullyMsg);
                return true;
            }
            catch (Exception e)
            {
                throw new SendMessageFailedException(e.Message, e);
            }
        }

        #endregion

        #region Actor life cycle method hooks

        protected override void PreRestart(Exception reason, object message)
        {
            _props.State.PendingMessages = 0;

            foreach (var pendingMsg in _props.State.ScheduledMessages)
            {
                pendingMsg.CancellationApparatus.Cancel();
                EnqueueOutgoingMessage(
                    pendingMsg.Message.OutgoingMessage
                );
            }

            _props.State.ScheduledMessages.Clear();

            switch (message)
            {
                case SendOutgoingMessageMessage msg:
                    msg.OutgoingMessage.SendErrors++;
                    if (msg.OutgoingMessage.SendErrors < 3)
                    {
                        EnqueueOutgoingMessage(msg.OutgoingMessage);
                    }
                    else
                    {
                        if (msg.OutgoingMessage.ScriptWaifu != null)
                            msg.OutgoingMessage.ScriptWaifu.Pending = false;
                    }
                    
                    var msgSendFailedMsg = new SendMessageFailedMessage(
                        msg.OutgoingMessage
                    );
                    Context.Parent.Tell(msgSendFailedMsg);
                    break;
            }

            base.PreRestart(reason, message);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Enqueues an <see cref="OutgoingMessage"/> into the appropriate queue.
        /// </summary>
        /// <param name="outgoingMessage"></param>
        private void EnqueueOutgoingMessage(
            OutgoingMessage outgoingMessage)
        {
            var msgQueue = GetOutgoingMessageQueue(outgoingMessage);
            msgQueue.Enqueue(outgoingMessage);
        }

        /// <summary>
        /// Gets the appropriate queue to place the <see cref="OutgoingMessage"/> into.
        /// </summary>
        /// <param name="outgoingMessage"></param>
        /// <returns></returns>
        private Queue<OutgoingMessage> GetOutgoingMessageQueue(OutgoingMessage outgoingMessage)
        {
            Queue<OutgoingMessage> msgQueue;
            switch (outgoingMessage.Flags)
            {
                case var x when x.HasFlag(OutgoingMessageFlags.Link):
                    msgQueue = _props.State.PendingLinkMessages;
                    break;

                case var x when x.HasFlag(OutgoingMessageFlags.Reply):
                    msgQueue = _props.State.PendingReplyMessages;
                    break;

                default:
                    msgQueue = _props.State.PendingGreetMessages;
                    break;
            }

            return msgQueue;
        }

        private void SchedulePendingMessages()
        {
            while (_props.State.PendingMessages < _props.MaxConcurrentReplies)
            {
                if (!TryGetNextMessage(out var outgoingMessage))
                    break;

                ScheduleSendMessageMessage(
                    Context.System.Scheduler,
                    Self,
                    outgoingMessage
                );
            }
        }

        /// <summary>
        /// Schedules a <see cref="SendOutgoingMessageMessage"/> to be sent to this actor.
        /// </summary>
        /// <param name="scheduler"></param>
        /// <param name="self"></param>
        /// <param name="outgoingMessage"></param>
        private void ScheduleSendMessageMessage(
            IScheduler scheduler,
            IActorRef self,
            OutgoingMessage outgoingMessage)
        {
            HandlePendingMessagesStateBeforeMessageScheduled();

            var timespan = GetMessageSendDelay(
                outgoingMessage
            );
            var sendMsgMsg = new SendOutgoingMessageMessage(
                outgoingMessage
            );

            var c = scheduler.ScheduleTellOnceCancelable(
                timespan,
                self,
                sendMsgMsg,
                self
            );
            var scheduled = new PendingCancelableOutgoingMessage(
                c,
                sendMsgMsg
            );
            _props.State.ScheduledMessages.Add(scheduled);
        }

        /// <summary>
        /// Gets the next <see cref="OutgoingMessage"/> placing the highest priority on
        /// messages with Link <see cref="OutgoingMessageFlags"/>.
        /// </summary>
        /// <param name="outgoingMessage"></param>
        /// <returns></returns>
        private bool TryGetNextMessage(out OutgoingMessage outgoingMessage)
        {
            if (_props.State.PendingLinkMessages.Count > 0)
            {
                outgoingMessage = _props.State.PendingLinkMessages.Dequeue();
                return true;
            }

            if (_props.State.PendingReplyMessages.Count > 0)
            {
                outgoingMessage = _props.State.PendingReplyMessages.Dequeue();
                return true;
            }

            if (_props.State.PendingGreetMessages.Count > 0)
            {
                outgoingMessage = _props.State.PendingGreetMessages.Dequeue();
                return true;
            }

            outgoingMessage = null;
            return false;
        }

        private void HandlePendingMessagesStateBeforeMessageScheduled()
        {
            // NOTE: this proprety is used to keep track of how many messages are in the process of sending.
            _props.State.PendingMessages++;

            if (_props.State.PendingMessages == _props.MaxConcurrentReplies &&
                _props.State.BehaviorState != MessageSenderActorBehaviorState.Working)
            {
                BecomeWorking();
            }

        }

        private void HandlePendingMessagesStateAfterMessageSent(
            IScheduler scheduler,
            IActorRef self)
        {
            _props.State.PendingMessages--;

            /* 
             * NOTE: If this method returns true, then there are messages the actor received
             * that have not been processed yet.  The actor will processe all of these before
             * decrementing the PendingMessages property and setting its behavior back to idle.
            */
            if (TryGetNextMessage(out var outgoingMessage))
            {
                ScheduleSendMessageMessage(
                    scheduler,
                    self,
                    outgoingMessage
                );
                return;
            }

            if (_props.State.BehaviorState != MessageSenderActorBehaviorState.ReadyToWork)
                BecomeReadyToWork();
        }

        private static TimeSpan GetMessageSendDelay(
            OutgoingMessage outgoingMessage)
        {
            var minDelayInSeconds = global::Waifu.Sys.Settings.Get<int>(
                Constants.MinMsgSendDelay
            );
            var maxDelayInSeconds = global::Waifu.Sys.Settings.Get<int>(
                Constants.MaxMsgSendDelay
            );

            if (minDelayInSeconds > maxDelayInSeconds)
            {
                minDelayInSeconds = 8;
                maxDelayInSeconds = 13;
            }

            var msgSendDelay = ThreadSafeStaticRandom.RandomInt(
                minDelayInSeconds,
                maxDelayInSeconds
            );
            var typingDelay = GetTypingDelaySeconds(outgoingMessage.Body);
            var secondsToDelay = typingDelay + msgSendDelay;

            var timespan = TimeSpan.FromSeconds(secondsToDelay);
            return timespan;
        }

        private static int GetTypingDelaySeconds(string msg)
        {
            var split = msg.Split(' ');
            var words = split.Length;
            var seconds = (words / 50.0) * 60;
            if (seconds > 25)
                seconds = 25;
            if (seconds < 15)
                seconds = 15;

            return (int)seconds;
        }

        public static Props CreateProps(
            MessageSenderPropsContainer propsContainer)
        {
            return Props.Create(
                () => new MessageSenderActor(propsContainer)
            );
        }

    #endregion
    }
}
