using System;
using Akka.Actor;
using Tumblr.Bot.Enums;
using Tumblr.Bot.Shikaka.Messages.ChatLogAppender;
using Tumblr.Bot.Shikaka.PropsContainers;
using Tumblr.Bot.UserInterface;

namespace Tumblr.Bot.Shikaka.Actors.Supervisor.ChatLogAppender
{
    internal class ChatLogAppenderActor : ReceiveActor
    {
        private readonly ChatLogAppenderPropsContainer _props;

        public ChatLogAppenderActor(
            ChatLogAppenderPropsContainer props)
        {
            _props = props;
            Receive<AppendChatLogMessage>(
                HandleAppendChatLogMessage
            );
        }

        private bool HandleAppendChatLogMessage(
            AppendChatLogMessage messageRcvd)
        {
            if (!global::Waifu.Sys.Settings.Get<bool>(Constants.ChatLogEnabled))
                return true;

            const string left = "<==";
            const string right = "==>";
            var arrow = messageRcvd.Direction == ChatLogMessageDirection.Incoming ? left : right;
            var str = $"[{DateTime.Now.ToShortTimeString()}] {messageRcvd.BotUsername} {arrow} {messageRcvd.ContactUsername}: {messageRcvd.Message}{Environment.NewLine}";
            _props.ChatLogTextBox.Dispatcher.Invoke(() => _props.ChatLogTextBox.AppendText(str));
            return true;
        }

        public static Props CreateProps(
            ChatLogAppenderPropsContainer propsContainer)
        {
            return Props.Create(
                () => new ChatLogAppenderActor(propsContainer)
            );
        }
    }
}
