using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Shikaka.Messages.SQLite;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.SQLite.Entities;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.SQLite
{
    internal class SQLiteActor : ReceiveActor
#if DEBUG
        , ILogReceive
#endif
    {
        private readonly SQLitePropsContainer _props;

        public SQLiteActor(
            SQLitePropsContainer propsContainer)
        {
            _props = propsContainer;

            ReceiveAsync<AddChatBlacklistEntityMessage>(
                HandleAddChatBlacklistItemMessage
            );
            ReceiveAsync<UpdateConvoEntityMessage>(
                HandleUpdateConvoEntityMessage
            );
            ReceiveAsync<AddNewConvoEntityMessage>(
                HandleAddNewConvoEntityMessage
            );
            ReceiveAsync<AddGreetBlacklistEntityMessage>(
                HandleAddGreetBlacklistEntityMessage
            );
        }

        private async Task<bool> HandleUpdateConvoEntityMessage(
            UpdateConvoEntityMessage messageRcvd)
        {
            var toUsername = messageRcvd.OutgoingMessage.ToUsername;
            var convoEntityKey = $"{messageRcvd.AccountUsername}~{toUsername}";
            if (!await _props.SQLiteDb.ConvoStatesTable.ContainsKeyAsync(
                convoEntityKey).ConfigureAwait(false))
            {
                return true;
            }

            var convo = messageRcvd.ScriptWaifu;
            var convoEntity = await _props.SQLiteDb.ConvoStatesTable.RetrieveByKeyAsync(
                convoEntityKey
            ).ConfigureAwait(false);
            convoEntity.ScriptIndex = convo.ScriptIndex;
            if (convo.IsComplete)
                convoEntity.IsComplete = true;

            await _props.SQLiteDb.ConvoStatesTable.UpdateAsync(
                convoEntity
            ).ConfigureAwait(false);

            return true;
        }

        private async Task<bool> HandleAddNewConvoEntityMessage(
            AddNewConvoEntityMessage messageRcvd)
        {
            var convoEntity = new ConversationStateEntity(
                messageRcvd.BotAccountUsername,
                messageRcvd.ContactUsername,
                messageRcvd.BotUuid,
                messageRcvd.ContactUuid,
                messageRcvd.ConversationId,
                messageRcvd.Sha256Sum
            );
            await _props.SQLiteDb.ConvoStatesTable.InsertOrReplaceAsync(convoEntity)
                .ConfigureAwait(false);
            return true;
        }

        private async Task<bool> HandleAddChatBlacklistItemMessage(
            AddChatBlacklistEntityMessage messageRcvd)
        {
            var entity = new BlacklistItemEntity(
                messageRcvd.Item
            );
            await _props.SQLiteDb.ChatBlacklistTable.InsertAsync(
                entity
            ).ConfigureAwait(false);

            return true;
        }

        private async Task<bool> HandleAddGreetBlacklistEntityMessage(
            AddGreetBlacklistEntityMessage messageRcvd)
        {
            var entity = new BlacklistItemEntity(
                messageRcvd.Item
            );
            await _props.SQLiteDb.GreetBlacklistTable.InsertAsync(
                entity
            ).ConfigureAwait(false);

            return true;
        }

        public static Props CreateProps(
            SQLitePropsContainer propsContainer)
        {
            return Props.Create(
                () => new SQLiteActor(propsContainer)
            );
        }
    }
}
