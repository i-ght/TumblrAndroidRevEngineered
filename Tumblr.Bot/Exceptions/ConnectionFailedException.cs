using System;

namespace Tumblr.Bot.Exceptions
{
    internal class ConnectionFailedException : InvalidOperationException
    {
        public ConnectionFailedException()
        {
        }

        public ConnectionFailedException(string message) : base(message)
        {
        }

        public ConnectionFailedException(
            string message,
            Exception innerException) : base(message, innerException)
        {
        }
    }
}
