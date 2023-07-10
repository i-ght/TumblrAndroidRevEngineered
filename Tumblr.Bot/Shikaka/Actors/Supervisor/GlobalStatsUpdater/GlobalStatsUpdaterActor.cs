using Akka.Actor;
using Tumblr.Bot.Enums;
using Tumblr.Bot.Shikaka.Messages.GlobalStatsUpdater;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.UserInterface;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.GlobalStatsUpdater
{
    internal class GlobalStatsUpdaterActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly GlobalStatsUpdaterPropsContainer _props;

        /// <inheritdoc />
        /// <summary>
        /// Creates a new instance of a <see cref="T:Tumblr.Bot.Shikaka.Actors.Supervisor.GlobalStatsUpdater.GlobalStatsUpdaterActor" />
        /// </summary>
        /// <param name="propsContainer"></param>
        public GlobalStatsUpdaterActor(
            GlobalStatsUpdaterPropsContainer propsContainer)
        {
            _props = propsContainer;
            Receive<UpdateGlobalStatMessage>(
                HandleGlobalStatMessage
            );
        }

        /// <summary>
        /// This method is invoked when the actor recieves a <see cref="UpdateGlobalStatMessage"/>.
        /// This message is received from a <see cref="T:Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.WorkerActor"/>
        /// The message handler increments or deincrements ints on the <see cref="GlobalStats"/> instance.
        /// </summary>
        /// <param name="msgRcvd"></param>
        /// <returns></returns>
        private bool HandleGlobalStatMessage(UpdateGlobalStatMessage msgRcvd)
        {
            switch (msgRcvd.StatKind)
            {
                case StatKind.Greets:
                    _props.State.GlobalStats.Greets =
                        (long)msgRcvd.IncrementOrDecrement * _props.State.GlobalStats.Greets + msgRcvd.Amount;
                    break;

                case StatKind.Convos:
                    _props.State.GlobalStats.Convos =
                        (long)msgRcvd.IncrementOrDecrement * _props.State.GlobalStats.Convos + msgRcvd.Amount;
                    break;

                case StatKind.In:
                    _props.State.GlobalStats.In =
                        (long)msgRcvd.IncrementOrDecrement * _props.State.GlobalStats.In + msgRcvd.Amount;
                    break;

                case StatKind.Out:
                    _props.State.GlobalStats.Out =
                        (long)msgRcvd.IncrementOrDecrement * _props.State.GlobalStats.Out + msgRcvd.Amount;
                    break;

                case StatKind.Links:
                    _props.State.GlobalStats.Links =
                        (long)msgRcvd.IncrementOrDecrement * _props.State.GlobalStats.Links + msgRcvd.Amount;
                    break;

                case StatKind.Completed:
                    _props.State.GlobalStats.Completed =
                        (long)msgRcvd.IncrementOrDecrement * _props.State.GlobalStats.Completed + msgRcvd.Amount;
                    break;

                case StatKind.Restricts:
                    _props.State.GlobalStats.Restricts =
                        (long)msgRcvd.IncrementOrDecrement * _props.State.GlobalStats.Restricts + msgRcvd.Amount;
                    break;
            }

            return true;
        }

        public static Props CreateProps(
            GlobalStatsUpdaterPropsContainer propsContainer)
        {
            return Props.Create(
                () => new GlobalStatsUpdaterActor(propsContainer)
            );
        }
    }
}
