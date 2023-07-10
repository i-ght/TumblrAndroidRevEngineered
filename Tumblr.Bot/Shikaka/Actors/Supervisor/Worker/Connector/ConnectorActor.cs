using System;
using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Exceptions;
using Tumblr.Bot.Shikaka.Messages.Connector;
using Tumblr.Bot.Shikaka.Messages.Worker;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Waifu.Exceptions;
using Waifu.Net.Http.Exceptions;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.Connector
{
    internal class ConnectorActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly ConnectorPropsContainer _props;

        public ConnectorActor(
            ConnectorPropsContainer propsContainer)
        {
            _props = propsContainer;
            ReceiveAsync<AttemptToConnectMessage>(
                HandleAttemptToConnectMessage
            );
        }

        private async Task<bool> HandleAttemptToConnectMessage(
            AttemptToConnectMessage messageRcvd)
        {
            try
            {
                var sender = Sender;
                await _props.Client.RetrieveUserInfo()
                    .ConfigureAwait(false);

                var succMsg = new ConnectionAttemptSucceededMessage();
                sender.Tell(succMsg);
                return true;
            }
            catch (Exception e)
            {
                throw CreateAppropriateException(e);
            }
        }

        private static Exception CreateAppropriateException(Exception e)
        {
            switch (e)
            {
                case HttpRequestFailedException httpEx:
                    switch (httpEx.HttpStatusCode)
                    {
                        case 0:
                            return new DeadProxyException(
                                $"{e.GetType().Name} ~ {e.Message}",
                                httpEx
                            );

                        default:
                            return new ConnectionFailedException(
                                $"{e.GetType().Name} ~ {e.Message}",
                                e
                            );
                    }

                default:
                    return new ConnectionFailedException(
                        $"{e.GetType().Name} ~ {e.Message}",
                        e
                    );
            }
        }

        public static Props CreateProps(
            ConnectorPropsContainer propsContainer)
        {
            return Props.Create(
                () => new ConnectorActor(propsContainer)
            );
        }
    }
}