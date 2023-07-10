using System;
using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Exceptions;
using Tumblr.Bot.Shikaka.Messages.SessionChecker;
using Tumblr.Bot.Shikaka.Messages.Worker;
using Tumblr.Bot.Shikaka.PropsContainers;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.SessionChecker
{
    internal class SessionCheckerActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly SessionCheckerPropsContainer _props;

        public SessionCheckerActor(
            SessionCheckerPropsContainer propsContainer)
        {
            _props = propsContainer;

            ReceiveAsync<AttemptCheckSessionMessage>(
                HandleAttemptCheckSessionMessage
            );
        }

        private async Task<bool> HandleAttemptCheckSessionMessage(
            AttemptCheckSessionMessage messageRcvd)
        {
            try
            {
                var sender = Sender;
                var responseContainer = await _props.Client.RetrieveUnreadMessagesCount()
                    .ConfigureAwait(false);

                var unreadCnt = 0;
                if (responseContainer.Response != null &&
                    responseContainer.Response.UnreadMessages != null)
                {
                    unreadCnt = responseContainer.Response.UnreadMessages.Count;
                }

                var succMsg = new SessionOkMessage(unreadCnt);
                sender.Tell(succMsg);
                return true;
            }
            catch (Exception e)
            {
                throw new SessionCheckFailedException(
                    e.Message,
                    e
                );
            }
        }


        public static Props CreateProps(
            SessionCheckerPropsContainer propsContainer)
        {
            return Props.Create(
                () => new SessionCheckerActor(propsContainer)
            );
        }
    }
}
