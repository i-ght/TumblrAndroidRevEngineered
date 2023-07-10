using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Shikaka.Messages.ConversationRetriever;
using Tumblr.Bot.Shikaka.PropsContainers;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.ConversationRetriever
{
    internal class ConversationRetrieverActor : ReceiveActor
    {
        private readonly ConversationRetrieverPropsContainer _props;

        public ConversationRetrieverActor(
            ConversationRetrieverPropsContainer propsContainer)
        {
            _props = propsContainer;
            ReceiveAsync<RetrieveConversationMessage>(
                HandleRetrieveConversationMessage
            );
        }

        private async Task<bool> HandleRetrieveConversationMessage(
            RetrieveConversationMessage messageRcvd)
        {
            await _props.Client.RetrieveConversation(
                messageRcvd.ConversatioId,
                messageRcvd.WithUuid
            ).ConfigureAwait(false);
            return true;
        }


        public static Props CreateProps(
            ConversationRetrieverPropsContainer propsContainer)
        {
            return Props.Create(
                () => new ConversationRetrieverActor(propsContainer)
            );
        }
    }
}
