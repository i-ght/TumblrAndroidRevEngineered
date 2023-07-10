using Tumblr.Bot.Enums;

namespace Tumblr.Bot.Shikaka.Messages.ChatLogAppender
{
    internal class AppendChatLogMessage
    {
        public AppendChatLogMessage(
            ChatLogMessageDirection direction,
            string botUsername,
            string contactUsername,
            string message)
        {
            Direction = direction;
            BotUsername = botUsername;
            ContactUsername = contactUsername;
            Message = message;
        }

        public ChatLogMessageDirection Direction { get; }
        public string BotUsername { get; }
        public string ContactUsername { get; }
        public string Message { get; }
    }
}
