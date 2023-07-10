using System.Threading.Tasks;
using Akka.Actor;
using Tumblr.Bot.Helpers;
using Tumblr.Bot.Shikaka.Messages.ContactReader;
using Tumblr.Bot.Shikaka.Messages.GreetSessionHandler;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.UserInterface;
using Waifu.Sys;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.ContactReader
{
    internal class ContactReaderActor : ReceiveActor
    {
        private readonly ContactReaderPropsContainer _props;

        public ContactReaderActor(
            ContactReaderPropsContainer propsContainer)
        {
            _props = propsContainer;

            ReceiveAsync<AdvanceContactStreamReaderToAnItemNotBlacklistedMessage>(
                HandleAdvanceContactStreamReaderToAnItemNotBlacklistedMessage
            );
            ReceiveAsync<GetNextContactMessage>(
                HandleGetNextContactMessage
            );
        }

        private async Task<bool> HandleAdvanceContactStreamReaderToAnItemNotBlacklistedMessage(
            AdvanceContactStreamReaderToAnItemNotBlacklistedMessage arg)
        {
            await ContactStreamHelpers.AdvanceContactStreamReaderToAnItemNotBlacklisted(
                _props.ContactStream.StreamReader,
                _props.SQLiteDb
            ).ConfigureAwait(false);
            return true;
        }

        private async Task<bool> HandleGetNextContactMessage(
            GetNextContactMessage messageRcvd)
        {
            var sender = Sender;

            if (global::Waifu.Sys.Settings.Get<bool>(Constants.DisableGreeting) ||
                _props.ContactStream.StreamReader.EndOfStream)
            {
                SendBlankContactMessage(sender);
                return true;
            }

            using (_props.SQLiteDb.GreetBlacklistTable.BeginTransaction())
            {
                while (!_props.ContactStream.StreamReader.EndOfStream)
                {
                    var line = await _props.ContactStream.StreamReader.ReadLineAsync()
                        .ConfigureAwait(false);
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var split = line.Split('|');
                    if (split.Length < 2)
                        continue;

                    if (StringHelpers.AnyNullOrWhitespace(split))
                        continue;

                    var username = split[0];

                    if (await _props.SQLiteDb.GreetBlacklistTable.ContainsItemAsync(username)
                        .ConfigureAwait(false))
                    {
                        continue;
                    }

                    if (await _props.SQLiteDb.ChatBlacklistTable.ContainsItemAsync(username)
                        .ConfigureAwait(false))
                    {
                        continue;
                    }

                    var uuid = split[1];

                    var contactMsg = new ContactMessage(
                        username,
                        uuid
                    );
                    sender.Tell(contactMsg);
                    return true;
                }
            }

            SendBlankContactMessage(sender);
            return true;
        }

        private void SendBlankContactMessage(
            IActorRef sender)
        {
            var contactMsg = new ContactMessage();
            sender.Tell(contactMsg);
        }

        public static Props CreateProps(
            ContactReaderPropsContainer propsContainer)
        {
            return Props.Create(
                () => new ContactReaderActor(propsContainer)
            );
        }
    }
}
