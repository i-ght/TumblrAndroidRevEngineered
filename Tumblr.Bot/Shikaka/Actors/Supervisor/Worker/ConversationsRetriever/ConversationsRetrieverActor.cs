using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Exceptions;
using Tumblr.Bot.Shikaka.Messages.ConversationsRetriever;
using Tumblr.Bot.Shikaka.Messages.Worker;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Waifu.JsonObjects;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.Worker.ConversationsRetriever
{
    internal class ConversationsRetrieverActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly ConversationsRetrieverPropsContainer _props;

        public ConversationsRetrieverActor(
            ConversationsRetrieverPropsContainer propsContainer)
        {
            _props = propsContainer;
            ReceiveAsync<RetrieveConversationsMessage>(
                HandleRetrieveConversationsMessage
            );
        }

        private async Task<bool> HandleRetrieveConversationsMessage(
            RetrieveConversationsMessage messagRcvd)
        {
            try
            {
                var sender = Sender;
                var responseContainer = await RetrieveConvos(
                    after: _props.State.After
                ).ConfigureAwait(false);
                var responseContainersList = new List<RetrieveConversationsTumblrApiResponse>
                {
                    responseContainer
                };

                var firstPage = true;
                var newAfter = string.Empty;
                while (
                    ConvoApiResponseHasNext(responseContainer))
                {
                    if (firstPage)
                    {
                        if (responseContainer.Response.Conversations != null &&
                            responseContainer.Response.Conversations.Count > 0)
                        {
                            var c = responseContainer.Response.Conversations;
                            newAfter = c[0].LastModifiedTs.ToString();
                        }
                        firstPage = false;
                    }

                    responseContainer = await RetrieveConvos(
                        responseContainer.Response.Links.Next.QueryParams.Before,
                        _props.State.After
                    ).ConfigureAwait(false);

                    responseContainersList.Add(responseContainer);
                }

                var convos = new List<TumblrConversation>();
                foreach (var respC in responseContainersList)
                {
                    if (respC.Response.Conversations.Count == 0)
                        continue;

                    convos.AddRange(
                        respC.Response.Conversations
                    );
                }

                if (!string.IsNullOrWhiteSpace(newAfter))
                    _props.State.After = newAfter;

                var convosRetrievedMsg = new ConversationsRetrievedMessage(
                    convos.AsReadOnly()
                );
                sender.Tell(convosRetrievedMsg);

                return true;
            }
            catch (Exception e)
            {
                throw new ConversationsRetrievalException(
                    $"{e.GetType().Name} ~ {e.Message}",
                    e
                );
            }
        }

        private static bool ConvoApiResponseHasNext(RetrieveConversationsTumblrApiResponse responseContainer)
        {
            return responseContainer.Response != null &&
                   responseContainer.Response.Conversations != null &&
                   responseContainer.Response.Conversations.Count >= 10 &&
                   responseContainer.Response.Links != null &&
                   responseContainer.Response.Links.Next != null &&
                   responseContainer.Response.Links.Next.QueryParams != null &&
                   !string.IsNullOrWhiteSpace(responseContainer.Response.Links.Next.QueryParams.Before);
        }

        private async Task<RetrieveConversationsTumblrApiResponse> RetrieveConvos(
            string before = "",
            string after = "")
        {
            var responseContainer = await _props.Client.RetrieveConversations(
                before,
                after
            ).ConfigureAwait(false);

            if (responseContainer.Response == null ||
                responseContainer.Response.Conversations == null)
            {
                throw new InvalidOperationException(
                    "Failed to parse conversations response."
                );
            }

            return responseContainer;
        }

        public static Props CreateProps(
            ConversationsRetrieverPropsContainer propsContainer)
        {
            return Props.Create(
                () => new ConversationsRetrieverActor(propsContainer)
            );
        }
    }
}
