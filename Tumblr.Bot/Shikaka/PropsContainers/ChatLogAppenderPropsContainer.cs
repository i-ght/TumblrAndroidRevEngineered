using System.Windows.Controls;

namespace Tumblr.Bot.Shikaka.PropsContainers
{
    internal class ChatLogAppenderPropsContainer
    {
        public ChatLogAppenderPropsContainer(
            TextBox chatLogTextBox)
        {
            ChatLogTextBox = chatLogTextBox;
        }

        public TextBox ChatLogTextBox { get; }
    }
}
