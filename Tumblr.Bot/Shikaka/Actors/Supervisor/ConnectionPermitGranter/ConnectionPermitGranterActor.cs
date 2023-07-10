using System;
using Akka.Actor;
using Tumblr.Bot.Shikaka.Messages.ConnectionPermitGranter;
using Tumblr.Bot.Shikaka.Messages.Worker;
using Tumblr.Bot.Shikaka.PropsContainers;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.ConnectionPermitGranter
{
    internal class ConnectionPermitGranterActor : ReceiveActor, IWithUnboundedStash
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly ConnectionPermintGranterPropsContainer _props;

        /// <inheritdoc />
        /// <summary>
        /// Creates a new instance of a <see cref="T:Tumblr.Bot.Shikaka.Actors.Supervisor.ConnectionPermitGranter.ConnectionPermitGranterActor" />.
        /// </summary>
        /// <param name="propsContainer"></param>
        public ConnectionPermitGranterActor(
            ConnectionPermintGranterPropsContainer propsContainer)
        {
            _props = propsContainer;
            Receive<RequestConnectionPermitMessage>(
                HandleRequestPermissionToConnectMessage
            );
            Receive<RelenquishConnectionPermitMessage>(
                HandleRelenquishPermissionToConnectMessage
            );
        }

        public IStash Stash { get; set; }

        /// <summary>
        /// This method is invoked when the actor recieves a <see cref="RequestConnectionPermitMessage"/>.
        /// This message is received from a 
        /// The message handler checks the <see cref="T:Tumblr.Bot.Shikaka.StateContainers.ConnectionPermitGranterStateContainer"/> to see if any connection slots
        /// are available.  If none are availabe, the message is stashed.  If one is available, the ConnectionSlotsAvailable proprety
        /// on the <see cref="T:Tumblr.Bot.Shikaka.StateContainers.ConnectionPermitGranterStateContainer"/> is decremented.
        /// </summary>
        /// <param name="messageRcvd"></param>
        /// <returns></returns>
        private bool HandleRequestPermissionToConnectMessage(
            RequestConnectionPermitMessage messageRcvd)
        {
            if (_props.State.ConnectionSlotsAvailable == 0)
            {
                Stash.Stash();
                return true;
            }

            _props.State.ConnectionSlotsAvailable--;

            var message = new ConnectionPermitGrantedMessage();
            Sender.Tell(message);
            return true;
        }

        /// <summary>
        /// This method is invoked when the actor recieves a <see cref="RelenquishConnectionPermitMessage"/>.
        /// This message is received from a 
        /// or a .
        /// The message handler increments the ConnectionSlotsAvailable property of this instances
        /// <see cref="T:Tumblr.Bot.Shikaka.StateContainers.ConnectionPermitGranterStateContainer"/> and then unstashes the next message.
        /// </summary>
        /// <param name="messageRcvd"></param>
        /// <returns></returns>
        private bool HandleRelenquishPermissionToConnectMessage(
            RelenquishConnectionPermitMessage messageRcvd)
        {
            if (_props.State.ConnectionSlotsAvailable < _props.MaxPermits)
                _props.State.ConnectionSlotsAvailable++;

            Stash.Unstash();
            return true;
        }

        protected override void PreRestart(Exception reason, object message)
        {
            _props.State.ConnectionSlotsAvailable = _props.MaxPermits;
            base.PreRestart(reason, message);
        }

        public static Props CreateProps(
            ConnectionPermintGranterPropsContainer propsContainer)
        {
            return Props.Create(
                () => new ConnectionPermitGranterActor(propsContainer)
            );
        }
    }
}
