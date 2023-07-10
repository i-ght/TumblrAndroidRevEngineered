using System;

namespace Tumblr.Bot.Exceptions
{
    internal class SendMessageFailedException : InvalidOperationException
    {
        public SendMessageFailedException()
        {
        }

        public SendMessageFailedException(string message) : base(message)
        {
        }

        public SendMessageFailedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
